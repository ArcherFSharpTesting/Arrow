﻿module Archer.Arrows.Internal

open System
open System.ComponentModel
open System.IO
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Archer
open Archer.Arrows
open Archer.CoreTypes.InternalTypes

type ExecutionResultsAccumulator<'a> =
    | Empty
    | SetupRun of result: Result<'a, SetupTeardownFailure>
    | TestRun of setupResult: Result<'a, SetupTeardownFailure> * testResult: TestResult
    | TeardownRun of setupResult: Result<'a, SetupTeardownFailure> * testResult: TestResult option * teardownResult: Result<unit, SetupTeardownFailure>
    | FailureAccumulated of setupResult: Result<'a, SetupTeardownFailure> option * GeneralTestingFailure 

type TestCaseExecutor<'a> (parent: ITest, setup: unit -> Result<'a, SetupTeardownFailure>, testBody: 'a -> TestEnvironment -> TestResult, tearDown: Result<'a, SetupTeardownFailure> -> TestResult option -> Result<unit, SetupTeardownFailure>) =
    let testLifecycleEvent = Event<TestExecutionDelegate, TestEventLifecycle> ()
    
    let getApiEnvironment () =
        let assembly = System.Reflection.Assembly.GetExecutingAssembly ()
        let version = assembly.GetName().Version
        
        {
            ApiName = "Archer.Arrows"
            ApiVersion = version
        }
        
    let executionStarted (cancelEventArgs: CancelEventArgs) =
        try
            testLifecycleEvent.Trigger (parent, TestStartExecution cancelEventArgs)
            cancelEventArgs, Empty
        with
        | ex -> cancelEventArgs, (None, ex |> GeneralExceptionFailure) |> FailureAccumulated
        
    let runSetup (cancelEventArgs: CancelEventArgs, acc) =
        if cancelEventArgs.Cancel then
            cancelEventArgs, acc
        else
            match acc with
            | Empty ->
                try
                    testLifecycleEvent.Trigger (parent, TestStartSetup cancelEventArgs)
                    
                    if cancelEventArgs.Cancel then
                        cancelEventArgs, acc
                    else
                        let result = () |> setup |> SetupRun
                        
                        let setupResult =
                            match result with
                            | SetupRun (Ok _) -> SetupSuccess
                            | SetupRun (Error errorValue) ->
                                errorValue |> SetupFailure
                            | _ -> failwith "Should not get here"
                        
                        testLifecycleEvent.Trigger (parent, TestEndSetup (setupResult, cancelEventArgs))
                        
                        cancelEventArgs, result
                with
                | ex ->
                    cancelEventArgs, ex |> SetupTeardownExceptionFailure |> Error |> SetupRun
            | _ -> cancelEventArgs, acc
        
    let runTestBody environment (cancelEventArgs: CancelEventArgs, acc) =
        if cancelEventArgs.Cancel then
            cancelEventArgs, acc
        else
            match acc with
            | SetupRun (Ok value as setupState) ->
                try
                    testLifecycleEvent.Trigger (parent, TestStart cancelEventArgs)
                    try
                        if cancelEventArgs.Cancel then
                            cancelEventArgs, acc
                        else
                            let testResult = environment |> testBody value
                            let result = (setupState, testResult) |> TestRun
                            
                            try
                                testLifecycleEvent.Trigger (parent, TestEnd testResult)
                                cancelEventArgs, result
                            with
                            | ex -> cancelEventArgs, (setupState |> Some, ex |> GeneralExceptionFailure) |> FailureAccumulated
                    with
                    | ex -> cancelEventArgs, (setupState, ex |> TestExceptionFailure |> TestFailure) |> TestRun
                with
                | ex -> cancelEventArgs, (setupState |> Some, ex |> GeneralExceptionFailure) |> FailureAccumulated
            | _ -> cancelEventArgs, acc
        
    let runTeardown setupResult testResult =
        try
            testLifecycleEvent.Trigger (parent, TestStartTeardown)
            
            let result = tearDown setupResult testResult
            let r = TeardownRun (setupResult, testResult, result)
            
            r
        with
        | ex ->
            TeardownRun (setupResult, testResult, ex |> SetupTeardownExceptionFailure |> Error)
        
    let maybeRunTeardown (cancelEventArgs: CancelEventArgs, acc) =
        match acc with
        | SetupRun setupResult ->
            cancelEventArgs, runTeardown setupResult None
        | TestRun (setupResult, testResult) ->
            cancelEventArgs, runTeardown setupResult (Some testResult)
        | FailureAccumulated (Some setupResult, _) ->
            let r = runTeardown setupResult None
            match r with
            | TeardownRun (_, _, Ok ()) ->
                cancelEventArgs, acc
            | TeardownRun (_, _, Error _) ->
                cancelEventArgs, r
            | _ -> failwith "should not get here"
        | FailureAccumulated (None, _) ->
            cancelEventArgs, acc
        | _ -> cancelEventArgs, acc
        
    member _.Execute environment =
        let env = 
            {
                ApiEnvironment = getApiEnvironment ()
                FrameworkEnvironment = environment
                TestInfo = parent 
            }
            
        let cancelEventArgs, result =
            CancelEventArgs ()
            |> executionStarted
            |> runSetup
            |> runTestBody env
            |> maybeRunTeardown
        
        let finalValue =
            match cancelEventArgs.Cancel, result with
            | true, _ -> GeneralCancelFailure |> GeneralExecutionFailure
            | _, FailureAccumulated (_, generalTestingFailure) ->
                generalTestingFailure |> GeneralExecutionFailure
            | _, SetupRun (Error error) ->
                error |> SetupExecutionFailure
            | _, TestRun (_, result) ->
                result |> TestExecutionResult
            | _, TeardownRun (_setupResult, _testResultOption, Error errorValue) ->
                errorValue
                |> TeardownExecutionFailure
            | _, TeardownRun (Error errorValue, _testResultOption, _teardownResult) ->
                errorValue
                |> SetupExecutionFailure
            | _, TeardownRun (Ok _, Some testResult, Ok _) ->
                testResult
                |> TestExecutionResult
            | _ -> failwith "Should never get here"
            
        let isEmpty value =
            match value with
            | Empty -> true
            | _ -> false

        try
            if cancelEventArgs.Cancel && result |> isEmpty then
                finalValue
            else
                testLifecycleEvent.Trigger (parent, TestEndExecution finalValue)
                finalValue
        with
        | ex -> ex |> GeneralExceptionFailure |> GeneralExecutionFailure
        
    override _.ToString () =
        $"%s{parent.ToString ()}.IExecutor"
    
    interface ITestExecutor with
        member this.Parent = parent
        
        member this.Execute environment = this.Execute environment
        
        [<CLIEvent>]
        member this.TestLifecycleEvent = testLifecycleEvent.Publish

type TestCase<'a> (containerPath: string, containerName: string, testName: string, setup: unit -> Result<'a, SetupTeardownFailure>, testBody: 'a -> TestEnvironment -> TestResult, tearDown: Result<'a, SetupTeardownFailure> -> TestResult option -> Result<unit, SetupTeardownFailure>, tags: TestTag seq, filePath: string, fileName: string,  lineNumber: int) =
    let location = {
        FilePath = filePath
        FileName = fileName
        LineNumber = lineNumber 
    }
    
    member _.ContainerPath with get () = containerPath
    member _.ContainerName with get () = containerName
    member _.TestName with get () = testName
    member _.Location with get () = location
    member _.Tags with get () = tags
        
    override _.ToString () =
        [
            containerPath
            containerName
            testName
        ]
        |> List.filter (String.IsNullOrWhiteSpace >> not)
        |> fun items -> String.Join (".", items)
    
    interface ITest with
        member this.ContainerName = this.ContainerName
        member this.ContainerPath = this.ContainerPath
        member this.GetExecutor() = TestCaseExecutor (this :> ITest, setup, testBody, tearDown) :> ITestExecutor
        member this.Location = this.Location
        member this.Tags = this.Tags
        member this.TestName = this.TestName
        
type Feature (featurePath, featureName) =
    let mutable tests: ITest list = []
    
    // --------- TEST TAGS ---------
    member _.Test<'a> (tags: TagsIndicator, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, teardown: TeardownIndicator<'a>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        let fileInfo = FileInfo fileFullName
        let filePath = fileInfo.Directory.FullName
        let fileName = fileInfo.Name
        
        let test =
            match tags, setup, testBody, teardown with
            | TestTags tags, Setup setup, TestBody testBody, Teardown teardown -> 
                TestCase (featurePath, featureName, testName, setup, testBody, teardown, tags, filePath, fileName, lineNumber) :> ITest
        
        tests <- test::tests
        test
            
    member this.Test<'a> (testName: string, tags: TagsIndicator, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, teardown: TeardownIndicator<'a>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, setup, testBody, teardown, testName, fileFullName, lineNumber)
    
    member this.Test (tags: TagsIndicator, testBody: TestBodyIndicator<unit>, teardown: TeardownIndicator<unit>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, Setup (fun _ -> Ok ()), testBody, teardown, testName, fileFullName, lineNumber)
    
    member this.Test (testName: string, tags: TagsIndicator, testBody: TestBodyIndicator<unit>, teardown: TeardownIndicator<unit>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, Setup (fun _ -> Ok ()), testBody, teardown, testName, fileFullName, lineNumber)
        
    member this.Test<'a> (tags: TagsIndicator, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, setup, testBody, Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
        
    member this.Test<'a> (testName: string, tags: TagsIndicator, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, setup, testBody, Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
        
    member this.Test (tags: TagsIndicator, testBody: TestEnvironment -> TestResult, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, Setup (fun _ -> Ok ()), TestBody (fun _ -> testBody), Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
        
    member this.Test (testName: string, tags: TagsIndicator, testBody: TestEnvironment -> TestResult, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, Setup (fun _ -> Ok ()), TestBody (fun _ -> testBody), Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
            
    // --------- SET UP ---------
    member this.Test<'a> (setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, teardown: TeardownIndicator<'a>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], setup, testBody, teardown, testName, fileFullName, lineNumber)
        
    member this.Test<'a> (testName: string, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, teardown: TeardownIndicator<'a>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], setup, testBody, teardown, testName, fileFullName, lineNumber)
        
    member this.Test<'a> (setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], setup, testBody, Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
        
    member this.Test<'a> (testName: string, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], setup, testBody, Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
        
    // --------- TEST BODY ---------
    member this.Test (testBody: TestBodyIndicator<unit>, teardown: TeardownIndicator<unit>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], Setup (fun _ -> Ok ()), testBody, teardown, testName, fileFullName, lineNumber)
        
    member this.Test (testName: string, testBody: TestBodyIndicator<unit>, teardown: TeardownIndicator<unit>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], Setup (fun _ -> Ok ()), testBody, teardown, testName, fileFullName, lineNumber)
            
    member this.Test (testBody: TestEnvironment -> TestResult, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], Setup (fun _ -> Ok ()), TestBody (fun _ -> testBody), Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
            
    member this.Test (testName: string, testBody: TestEnvironment -> TestResult, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], Setup (fun _ -> Ok ()), TestBody (fun _ -> testBody), Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
            
    member this.GetTests () = tests
        
    override _.ToString () =
        [
            featurePath
            featureName
        ]
        |> List.filter (String.IsNullOrWhiteSpace >> not)
        |> fun items -> String.Join (".", items)
        
type TypedFeature<'setupType> (featurePath, featureName, defaultSetup: SetupIndicator<'setupType>, defaultTeardown: TeardownIndicator<'setupType>) =
    let mutable tests: ITest list = []
    
    // --------- TEST TAGS ---------
    member _.Test<'a> (tags: TagsIndicator, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, teardown: TeardownIndicator<'a>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        let fileInfo = FileInfo fileFullName
        let filePath = fileInfo.Directory.FullName
        let fileName = fileInfo.Name
        
        let test =
            match tags, setup, testBody, teardown with
            | TestTags tags, Setup setup, TestBody testBody, Teardown teardown -> 
                TestCase (featurePath, featureName, testName, setup, testBody, teardown, tags, filePath, fileName, lineNumber) :> ITest
        
        tests <- test::tests
        test
            
    member this.Test<'a> (testName: string, tags: TagsIndicator, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, teardown: TeardownIndicator<'a>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, setup, testBody, teardown, testName, fileFullName, lineNumber)
    
    member this.Test (tags: TagsIndicator, testBody: TestBodyIndicator<'setupType>, teardown: TeardownIndicator<'setupType>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, defaultSetup, testBody, teardown, testName, fileFullName, lineNumber)
    
    member this.Test (testName: string, tags: TagsIndicator, testBody: TestBodyIndicator<'setupType>, teardown: TeardownIndicator<'setupType>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, defaultSetup, testBody, teardown, testName, fileFullName, lineNumber)
        
    member this.Test<'a> (tags: TagsIndicator, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, setup, testBody, Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
        
    member this.Test<'a> (testName: string, tags: TagsIndicator, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, setup, testBody, Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
        
    member this.Test (tags: TagsIndicator, testBody: 'setupType -> TestEnvironment -> TestResult, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, defaultSetup, TestBody testBody, defaultTeardown, testName, fileFullName, lineNumber)
        
    member this.Test (testName: string, tags: TagsIndicator, testBody: 'setupType -> TestEnvironment -> TestResult, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (tags, defaultSetup, TestBody testBody, defaultTeardown, testName, fileFullName, lineNumber)
            
    // --------- SET UP ---------
    member this.Test<'a> (setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, teardown: TeardownIndicator<'a>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], setup, testBody, teardown, testName, fileFullName, lineNumber)
        
    member this.Test<'a> (testName: string, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, teardown: TeardownIndicator<'a>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], setup, testBody, teardown, testName, fileFullName, lineNumber)
        
    member this.Test<'a> (setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], setup, testBody, Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
        
    member this.Test<'a> (testName: string, setup: SetupIndicator<'a>, testBody: TestBodyIndicator<'a>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], setup, testBody, Teardown (fun _ _ -> Ok ()), testName, fileFullName, lineNumber)
        
    // --------- TEST BODY ---------
    member this.Test (testBody: TestBodyIndicator<'setupType>, teardown: TeardownIndicator<'setupType>, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], defaultSetup, testBody, teardown, testName, fileFullName, lineNumber)
        
    member this.Test (testName: string, testBody: TestBodyIndicator<'setupType>, teardown: TeardownIndicator<'setupType>, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], defaultSetup, testBody, teardown, testName, fileFullName, lineNumber)
            
    member this.Test (testBody: 'setupType -> TestEnvironment -> TestResult, [<CallerMemberName; Optional; DefaultParameterValue("")>] testName: string, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], defaultSetup, TestBody testBody, defaultTeardown, testName, fileFullName, lineNumber)
            
    member this.Test (testName: string, testBody: 'setupType -> TestEnvironment -> TestResult, [<CallerFilePath; Optional; DefaultParameterValue("")>] fileFullName: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>]lineNumber: int) =
        this.Test (TestTags [], defaultSetup, TestBody testBody, defaultTeardown, testName, fileFullName, lineNumber)
            
    member this.GetTests () = tests
        
    override _.ToString () =
        [
            featurePath
            featureName
        ]
        |> List.filter (String.IsNullOrWhiteSpace >> not)
        |> fun items -> String.Join (".", items)