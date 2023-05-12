﻿module Archer.Arrows.Tests.Feature.``Test Data Method name first with environment should``

open Archer
open Archer.Arrows
open Archer.Arrows.Internal.Types
open Archer.Arrows.Internals
open Archer.Arrows.Tests
open Archer.CoreTypes.InternalTypes
open Archer.MicroLang.Verification

let private feature = Arrow.NewFeature (
    TestTags [
        Category "Feature"
        Category "Test"
    ]
)

// Tags, Setup, TestBody, Teardown 
let ``return an ITest with everything when everything is passed`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testTags = 
                [
                   Only
                   Category "My Category"
               ]
            let testName = "Bogo %i"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let tests =
                testFeature.Test (
                    testName,
                    TestTags testTags,
                    Setup (fun _ -> Ok ()),
                    Data [6; 4; 3],
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    emptyTeardown,
                    fullPath,
                    lineNumber
                )
        
            let getContainerName (test: ITest) =
                $"%s{test.ContainerPath}.%s{test.ContainerName}"
                
            tests
            |> Should.PassAllOf [
                List.length >> Should.BeEqualTo 3 >> withMessage "Incorrect number of tests"
                
                List.head >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.head >> getTestName >> Should.BeEqualTo "Bogo 6" >> withMessage "TestName"
                List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.skip 1 >> List.head >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "Bogo 4" >> withMessage "TestName"
                List.skip 1 >> List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.skip 1 >> List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.skip 1 >> List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.skip 1 >> List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.last >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.last >> getTestName >> Should.BeEqualTo "Bogo 3" >> withMessage "TestName"
                List.last >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.last >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.last >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.last >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
            ]
        )
    )
    
let ``return an ITest with everything when everything is passed no name hints`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testTags = 
                [
                   Only
                   Category "My Category"
               ]
            let testName = "Bogo"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let tests =
                testFeature.Test (
                    testName,
                    TestTags testTags,
                    Setup (fun _ -> Ok ()),
                    Data [6; 4; 3],
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    emptyTeardown,
                    fullPath,
                    lineNumber
                )
        
            tests
            |> Should.PassAllOf [
                List.head >> getTestName >> Should.BeEqualTo "Bogo (6)" >> withMessage "TestName"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "Bogo (4)" >> withMessage "TestName"
                List.last >> getTestName >> Should.BeEqualTo "Bogo (3)" >> withMessage "TestName"
            ]
        )
    )
    
let ``run setup method passed to it when everything is passed`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let tests =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Setup monitor.CallSetup,
                    Data [11; -2; 44],
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    emptyTeardown,
                    "D:\\dog.bark",
                    73
                )
                
            tests
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesSetupWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Setup was not called"
        )
    )
    
let ``run the test method passed to it when everything is passed`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Setup monitor.CallSetup,
                    Data (seq{ 5..5..16 }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    Teardown monitor.CallTeardown,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesTestWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Setup was not called"
        )
    )
    
let ``run the test method passed to it when everything is passed by calling it with test data`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Setup monitor.CallSetup,
                    Data (seq{ 5..5..16 }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    Teardown monitor.CallTeardown,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.TestDataWas
            |> Should.BeEqualTo [5; 10; 15]
            |> withMessage "Setup was not called"
        )
    )
    
let ``run the teardown method passed to it when everything is passed`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<char, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Setup monitor.CallSetup,
                    Data (seq{ 'w'..'y' }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    Teardown monitor.CallTeardown,
                    "My test",
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesTeardownWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Teardown was not called"
        )
    )

// Tags, Setup, TestBody!
let ``return an ITest with everything when given no teardown`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testTags = 
                [
                   Only
                   Category "My Category"
               ]
            let testName = "At the %s"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let test =
                testFeature.Test (
                    testName,
                    TestTags testTags,
                    Setup (fun _ -> Ok ()),
                    Data ["park"; "pool"; "eatery"],
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    fullPath,
                    lineNumber
                )
        
            let getContainerName (test: ITest) =
                $"%s{test.ContainerPath}.%s{test.ContainerName}"
                
            test
            |> Should.PassAllOf [
                List.length >> Should.BeEqualTo 3 >> withMessage "Incorrect number of tests"
                
                List.head >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.head >> getTestName >> Should.BeEqualTo "At the park" >> withMessage "TestName"
                List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.skip 1 >> List.head >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "At the pool" >> withMessage "TestName"
                List.skip 1 >> List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.skip 1 >> List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.skip 1 >> List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.skip 1 >> List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.last >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.last >> getTestName >> Should.BeEqualTo "At the eatery" >> withMessage "TestName"
                List.last >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.last >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.last >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.last >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
            ]
        )
    )
    
let ``return an ITest with everything when given no teardown, no name hints`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testTags = 
                [
                   Only
                   Category "My Category"
               ]
            let testName = "At the"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let test =
                testFeature.Test (
                    testName,
                    TestTags testTags,
                    Setup (fun _ -> Ok ()),
                    Data ["park"; "pool"; "eatery"],
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    fullPath,
                    lineNumber
                )
        
            test
            |> Should.PassAllOf [
                List.head >> getTestName >> Should.BeEqualTo "At the (\"park\")" >> withMessage "TestName"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "At the (\"pool\")" >> withMessage "TestName"
                List.last >> getTestName >> Should.BeEqualTo "At the (\"eatery\")" >> withMessage "TestName"
            ]
        )
    )
    
let ``run setup method passed to it when given no teardown`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<unit, unit, unit> (Ok ())
            let tests =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Setup monitor.CallSetup,
                    Data (seq { 9..3..16 }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    "D:\\dog.bark",
                    73
                )
                
            tests
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesSetupWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Setup was not called"
        )
    )
    
let ``run the test method passed to it when given no teardown`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Setup monitor.CallSetup,
                    Data (seq{ 1..3 }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesTestWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Test was not called"
        )
    )
    
let ``run the test method passed to it when given no teardown by calling it with test data`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Setup monitor.CallSetup,
                    Data (seq{ 10..10..31 }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.TestDataWas
            |> Should.BeEqualTo [10; 20; 30]
            |> withMessage "Test was not called"
        )
    )

// Tags, TestBody, Teardown!
let ``return an ITest with everything when given no setup`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testTags = 
                [
                   Only
                   Category "My Category"
               ]
            let testName = "Test %i done"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let test =
                testFeature.Test (
                    testName,
                    TestTags testTags,
                    Data (seq{ 10..13..37 }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    emptyTeardown,
                    fullPath,
                    lineNumber
                )
        
            let getContainerName (test: ITest) =
                $"%s{test.ContainerPath}.%s{test.ContainerName}"
                
            test
            |> Should.PassAllOf [
                List.length >> Should.BeEqualTo 3 >> withMessage "Incorrect number of tests"
                
                List.head >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.head >> getTestName >> Should.BeEqualTo "Test 10 done" >> withMessage "TestName"
                List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.skip 1 >> List.head >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "Test 23 done" >> withMessage "TestName"
                List.skip 1 >> List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.skip 1 >> List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.skip 1 >> List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.skip 1 >> List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.last >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.last >> getTestName >> Should.BeEqualTo "Test 36 done" >> withMessage "TestName"
                List.last >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.last >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.last >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.last >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
            ]
        )
    )
    
let ``return an ITest with everything when given no setup, no name hints`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testTags = 
                [
                   Only
                   Category "My Category"
               ]
            let testName = "Test"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let tests =
                testFeature.Test (
                    testName,
                    TestTags testTags,
                    Data (seq{ 10..13..37 }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    emptyTeardown,
                    fullPath,
                    lineNumber
                )
        
            tests
            |> Should.PassAllOf [
                List.head >> getTestName >> Should.BeEqualTo "Test (10)" >> withMessage "TestName"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "Test (23)" >> withMessage "TestName"
                List.last >> getTestName >> Should.BeEqualTo "Test (36)" >> withMessage "TestName"
            ]
        )
    )
    
let ``run the test method passed to it when given no setup`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<char, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Data (seq{ 'q'..'s' }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    Teardown monitor.CallTeardown,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesTestWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Test was not called"
        )
    )
    
let ``run the test method passed to it when given no setup by calling it with test data`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<char, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Data (seq{ 'q'..'s' }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    Teardown monitor.CallTeardown,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.TestDataWas
            |> Should.BeEqualTo ['q'; 'r'; 's']
            |> withMessage "Test was not called"
        )
    )
    
let ``run the teardown method passed to it when given no setup`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Data (seq { -1..5..10 }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    Teardown monitor.CallTeardown,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesTeardownWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Teardown was not called"
        )
    )

// Tags, TestBody!
let ``return an ITest with everything when given no setup or teardown`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testTags = 
                [
                   Only
                   Category "My Category"
               ]
            let testName = "%c test"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let tests =
                testFeature.Test (
                    testName,
                    TestTags testTags,
                    Data (seq{ 'a'..'c' }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    fullPath,
                    lineNumber
                )
        
            let getContainerName (test: ITest) =
                $"%s{test.ContainerPath}.%s{test.ContainerName}"
                
            tests
            |> Should.PassAllOf [
                List.length >> Should.BeEqualTo 3 >> withMessage "Incorrect number of tests"
                
                List.head >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.head >> getTestName >> Should.BeEqualTo "a test" >> withMessage "TestName"
                List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.skip 1 >> List.head >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "b test" >> withMessage "TestName"
                List.skip 1 >> List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.skip 1 >> List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.skip 1 >> List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.skip 1 >> List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.last >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.last >> getTestName >> Should.BeEqualTo "c test" >> withMessage "TestName"
                List.last >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.last >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.last >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.last >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
            ]
        )
    )
    
let ``return an ITest with everything when given no setup, no teardown, no name hint`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testTags = 
                [
                   Only
                   Category "My Category"
               ]
            let testName = "Test"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let tests =
                testFeature.Test (
                    testName,
                    TestTags testTags,
                    Data (seq{ 'a'..'c' }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    fullPath,
                    lineNumber
                )
        
            tests
            |> Should.PassAllOf [
                List.head >> getTestName >> Should.BeEqualTo "Test ('a')" >> withMessage "TestName"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "Test ('b')" >> withMessage "TestName"
                List.last >> getTestName >> Should.BeEqualTo "Test ('c')" >> withMessage "TestName"
            ]
        )
    )
    
let ``run the test method passed to it when given no setup or teardown`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Data (seq{ 1..3 }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesTestWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Test was not called"
        )
    )
    
let ``run the test method passed to it when given no setup or teardown by calling it with test data`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Data (seq{ 1..3 }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.TestDataWas
            |> Should.BeEqualTo [1; 2; 3]
            |> withMessage "Test was not called"
        )
    )

let ``return an ITest with everything when given no setup, teardown, or test body indicator`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testTags = 
                [
                   Only
                   Category "My Category"
               ]
            let testName = "My test %i"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let tests =
                testFeature.Test (
                    testName,
                    TestTags testTags,
                    Data (seq{ 1..3 }),
                    (fun _ _ _ -> TestSuccess),
                    fullPath,
                    lineNumber
                )
        
            let getContainerName (test: ITest) =
                $"%s{test.ContainerPath}.%s{test.ContainerName}"
                
            tests
            |> Should.PassAllOf [
                List.length >> Should.BeEqualTo 3 >> withMessage "Incorrect number of tests"
                
                List.head >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.head >> getTestName >> Should.BeEqualTo "My test 1" >> withMessage "TestName"
                List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.skip 1 >> List.head >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "My test 2" >> withMessage "TestName"
                List.skip 1 >> List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.skip 1 >> List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.skip 1 >> List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.skip 1 >> List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.last >> getTags >> Should.BeEqualTo testTags >> withMessage "Tags"
                List.last >> getTestName >> Should.BeEqualTo "My test 3" >> withMessage "TestName"
                List.last >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.last >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.last >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.last >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
            ]
        )
    )

let ``return an ITest with everything when given no setup, no teardown, no test body indicator, and no name hints`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testTags = 
                [
                   Only
                   Category "My Category"
               ]
            let testName = "My test"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let tests =
                testFeature.Test (
                    testName,
                    TestTags testTags,
                    Data (seq{ 1..3 }),
                    (fun _ _ _ -> TestSuccess),
                    fullPath,
                    lineNumber
                )
        
            tests
            |> Should.PassAllOf [
                List.head >> getTestName >> Should.BeEqualTo "My test (1)" >> withMessage "TestName"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "My test (2)" >> withMessage "TestName"
                List.last >> getTestName >> Should.BeEqualTo "My test (3)" >> withMessage "TestName"
            ]
        )
    )
    
let ``run the test method passed to it when given no setup, teardown, or test body indicator`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Data (seq {1..3}),
                    monitor.CallTestActionWithDataSetupEnvironment,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesTestWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Test was not called"
        )
    )
    
let ``run the test method passed to it when given no setup, teardown, or test body indicator by calling it with test data`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    TestTags [
                                Only
                                Category "My Category"
                            ],
                    Data (seq {1..3}),
                    monitor.CallTestActionWithDataSetupEnvironment,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.TestDataWas
            |> Should.BeEqualTo [1; 2; 3]
            |> withMessage "Test was not called"
        )
    )

// Setup, TestBody, Teardown
let ``return an ITest with everything when given no tags`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testName = "My test %i"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let tests =
                testFeature.Test (
                    testName,
                    Setup (fun _ -> Ok ()),
                    Data (seq{ 1..3 }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    emptyTeardown,
                    fullPath,
                    lineNumber
                )
        
            let getContainerName (test: ITest) =
                $"%s{test.ContainerPath}.%s{test.ContainerName}"
                
            tests
            |> Should.PassAllOf [
                List.length >> Should.BeEqualTo 3 >> withMessage "Incorrect number of tests"
                
                List.head >> getTags >> Should.BeEqualTo [] >> withMessage "Tags"
                List.head >> getTestName >> Should.BeEqualTo "My test 1" >> withMessage "TestName"
                List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.skip 1 >> List.head >> getTags >> Should.BeEqualTo [] >> withMessage "Tags"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "My test 2" >> withMessage "TestName"
                List.skip 1 >> List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.skip 1 >> List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.skip 1 >> List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.skip 1 >> List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.last >> getTags >> Should.BeEqualTo [] >> withMessage "Tags"
                List.last >> getTestName >> Should.BeEqualTo "My test 3" >> withMessage "TestName"
                List.last >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.last >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.last >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.last >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
            ]
        )
    )
    
let ``return an ITest with everything when given no tags by calling it with test data`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testName = "My test"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let tests =
                testFeature.Test (
                    testName,
                    Setup (fun _ -> Ok ()),
                    Data (seq{ 1..3 }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    emptyTeardown,
                    fullPath,
                    lineNumber
                )
        
            tests
            |> Should.PassAllOf [
                List.head >> getTestName >> Should.BeEqualTo "My test (1)" >> withMessage "TestName"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "My test (2)" >> withMessage "TestName"
                List.last >> getTestName >> Should.BeEqualTo "My test (3)" >> withMessage "TestName"
            ]
        )
    )
    
let ``run setup method passed to it when given no tags`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<unit, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    Setup monitor.CallSetup,
                    Data (seq{ 11..13 }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    emptyTeardown,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesSetupWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Setup was not called"
        )
    )
    
let ``run the test method passed to it when given no tags`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    Setup monitor.CallSetup,
                    Data (seq{ 11..13 }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    Teardown monitor.CallTeardown,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesTestWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "test was not called"
        )
    )
    
let ``run the test method passed to it when given no tags by calling it with test data`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    Setup monitor.CallSetup,
                    Data (seq{ 11..13 }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    Teardown monitor.CallTeardown,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.TestDataWas
            |> Should.BeEqualTo [11; 12; 13]
            |> withMessage "test was not called"
        )
    )
    
let ``run the teardown method passed to it when given no tags`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<int, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    Setup monitor.CallSetup,
                    Data (seq { 13..16 }),
                    TestBodyThreeParameters monitor.CallTestActionWithDataSetupEnvironment,
                    Teardown monitor.CallTeardown,
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesTeardownWasCalled
            |> Should.BeEqualTo 4
            |> withMessage "teardown was not called"
        )
    )

// Setup, TestBody
let ``return an ITest with everything when given no tags, no teardown`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testName = "My test %i"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let test =
                testFeature.Test (
                    testName,
                    Setup (fun _ -> Ok ()),
                    Data (seq{ 119..121 }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    fullPath,
                    lineNumber
                )
        
            let getContainerName (test: ITest) =
                $"%s{test.ContainerPath}.%s{test.ContainerName}"
                
            test
            |> Should.PassAllOf [
                List.length >> Should.BeEqualTo 3 >> withMessage "Incorrect number of tests"
                
                List.head >> getTags >> Should.BeEqualTo [] >> withMessage "Tags"
                List.head >> getTestName >> Should.BeEqualTo "My test 119" >> withMessage "TestName"
                List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.skip 1 >> List.head >> getTags >> Should.BeEqualTo [] >> withMessage "Tags"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "My test 120" >> withMessage "TestName"
                List.skip 1 >> List.head >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.skip 1 >> List.head >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.skip 1 >> List.head >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.skip 1 >> List.head >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
                
                List.last >> getTags >> Should.BeEqualTo [] >> withMessage "Tags"
                List.last >> getTestName >> Should.BeEqualTo "My test 121" >> withMessage "TestName"
                List.last >> getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
                List.last >> getFilePath >> Should.BeEqualTo path >> withMessage "file path"
                List.last >> getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
                List.last >> getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
            ]
        )
    )
    
let ``return an ITest with everything when given no tags, no teardown, no name hints`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let testName = "My test"
            
            let fileName = "dog.bark"
            let path = "D:\\"
            let fullPath = $"%s{path}%s{fileName}"
            let lineNumber = 73
            
            let test =
                testFeature.Test (
                    testName,
                    Setup (fun _ -> Ok ()),
                    Data (seq{ 119..121 }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    fullPath,
                    lineNumber
                )
        
            test
            |> Should.PassAllOf [
                List.head >> getTestName >> Should.BeEqualTo "My test (119)" >> withMessage "TestName"
                List.skip 1 >> List.head >> getTestName >> Should.BeEqualTo "My test (120)" >> withMessage "TestName"
                List.last >> getTestName >> Should.BeEqualTo "My test (121)" >> withMessage "TestName"
            ]
        )
    )
    
let ``run setup method passed to it when given no tags, no teardown`` =
    feature.Test (
        Setup setupFeatureUnderTest,
        TestBody (fun (testFeature: IFeature<unit>) ->
            let monitor = Monitor<unit, unit, unit> (Ok ())
            let test =
                testFeature.Test (
                    "My test",
                    Setup monitor.CallSetup,
                    Data (seq{ 749..751 }),
                    TestBodyThreeParameters (fun _ _ _ -> TestSuccess),
                    "D:\\dog.bark",
                    73
                )
                
            test
            |> silentlyRunAllTests
            
            monitor.NumberOfTimesSetupWasCalled
            |> Should.BeEqualTo 3
            |> withMessage "Setup was not called"
        )
    )
    
// let ``run the test method passed to it when given no tags, no teardown`` =
//     feature.Test (
//         Setup setupFeatureUnderTest,
//         TestBody (fun (testFeature: IFeature<unit>) ->
//             let monitor = Monitor<unit, unit, unit> (Ok ())
//             let test =
//                 testFeature.Test (
//                     "My test",
//                     Setup monitor.CallSetup,
//                     TestBodyTwoParameters monitor.CallTestActionWithSetupEnvironment,
//                     "D:\\dog.bark",
//                     73
//                 )
//                 
//             test
//             |> silentlyRunTest
//             
//             monitor.TestWasCalled
//             |> Should.BeTrue
//             |> withMessage "test was not called"
//         )
//     )
//
// // TestBody, Teardown
// let ``return an ITest with everything when given no tags, no setup`` =
//     feature.Test (
//         Setup setupFeatureUnderTest,
//         TestBody (fun (testFeature: IFeature<unit>) ->
//             let testName = "My test"
//             
//             let fileName = "dog.bark"
//             let path = "D:\\"
//             let fullPath = $"%s{path}%s{fileName}"
//             let lineNumber = 73
//             
//             let test =
//                 testFeature.Test (
//                     testName,
//                     TestBodyTwoParameters (fun _ _ -> TestSuccess),
//                     emptyTeardown,
//                     fullPath,
//                     lineNumber
//                 )
//         
//             let getContainerName (test: ITest) =
//                 $"%s{test.ContainerPath}.%s{test.ContainerName}"
//                 
//             test
//             |> Should.PassAllOf [
//                 getTags >> Should.BeEqualTo [] >> withMessage "Tags"
//                 getTestName >> Should.BeEqualTo testName >> withMessage "TestName"
//                 getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
//                 getFilePath >> Should.BeEqualTo path >> withMessage "file path"
//                 getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
//                 getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
//             ]
//         )
//     )
//     
// let ``run the test method passed to it when given no tags, no setup`` =
//     feature.Test (
//         Setup setupFeatureUnderTest,
//         TestBody (fun (testFeature: IFeature<unit>) ->
//             let monitor = Monitor<unit, unit, unit> (Ok ())
//             let test =
//                 testFeature.Test (
//                     "My test",
//                     TestBodyTwoParameters monitor.CallTestActionWithSetupEnvironment,
//                     Teardown monitor.CallTeardown,
//                     "D:\\dog.bark",
//                     73
//                 )
//                 
//             test
//             |> silentlyRunTest
//             
//             monitor.TestWasCalled
//             |> Should.BeTrue
//             |> withMessage "test was not called"
//         )
//     )
//     
// let ``run the teardown method passed to it when given no tags, no setup`` =
//     feature.Test (
//         Setup setupFeatureUnderTest,
//         TestBody (fun (testFeature: IFeature<unit>) ->
//             let monitor = Monitor<unit, unit, unit> (Ok ())
//             let test =
//                 testFeature.Test (
//                     "My test",
//                     TestBodyTwoParameters monitor.CallTestActionWithSetupEnvironment,
//                     Teardown monitor.CallTeardown,
//                     "D:\\dog.bark",
//                     73
//                 )
//                 
//             test
//             |> silentlyRunTest
//             
//             monitor.TeardownWasCalled
//             |> Should.BeTrue
//             |> withMessage "teardown was not called"
//         )
//     )
//
// // TestBody
// let ``return an ITest with everything when given no tags, no setup, no teardown`` =
//     feature.Test (
//         Setup setupFeatureUnderTest,
//         TestBody (fun (testFeature: IFeature<unit>) ->
//             let testName = "My test"
//             
//             let fileName = "dog.bark"
//             let path = "D:\\"
//             let fullPath = $"%s{path}%s{fileName}"
//             let lineNumber = 73
//             
//             let test =
//                 testFeature.Test (
//                     testName,
//                     TestBodyTwoParameters (fun _ _ -> TestSuccess),
//                     fullPath,
//                     lineNumber
//                 )
//         
//             let getContainerName (test: ITest) =
//                 $"%s{test.ContainerPath}.%s{test.ContainerName}"
//                 
//             test
//             |> Should.PassAllOf [
//                 getTags >> Should.BeEqualTo [] >> withMessage "Tags"
//                 getTestName >> Should.BeEqualTo testName >> withMessage "TestName"
//                 getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
//                 getFilePath >> Should.BeEqualTo path >> withMessage "file path"
//                 getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
//                 getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
//             ]
//         )
//     )
//     
// let ``run the test method passed to it when given no tags, no setup, no teardown`` =
//     feature.Test (
//         Setup setupFeatureUnderTest,
//         TestBody (fun (testFeature: IFeature<unit>) ->
//             let monitor = Monitor<unit, unit, unit> (Ok ())
//             let test =
//                 testFeature.Test (
//                     "My test",
//                     TestBodyTwoParameters monitor.CallTestActionWithSetupEnvironment,
//                     "D:\\dog.bark",
//                     73
//                 )
//                 
//             test
//             |> silentlyRunTest
//             
//             monitor.TestWasCalled
//             |> Should.BeTrue
//             |> withMessage "test was not called"
//         )
//     )
//
// let ``return an ITest with everything when given no tags, no setup, no teardown, no test body indicator`` =
//     feature.Test (
//         Setup setupFeatureUnderTest,
//         TestBody (fun (testFeature: IFeature<unit>) ->
//             let testName = "My test"
//             
//             let fileName = "dog.bark"
//             let path = "D:\\"
//             let fullPath = $"%s{path}%s{fileName}"
//             let lineNumber = 73
//             
//             let test =
//                 testFeature.Test (
//                     testName,
//                     (fun _ _ -> TestSuccess),
//                     fullPath,
//                     lineNumber
//                 )
//         
//             let getContainerName (test: ITest) =
//                 $"%s{test.ContainerPath}.%s{test.ContainerName}"
//                 
//             test
//             |> Should.PassAllOf [
//                 getTags >> Should.BeEqualTo [] >> withMessage "Tags"
//                 getTestName >> Should.BeEqualTo testName >> withMessage "TestName"
//                 getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Information"
//                 getFilePath >> Should.BeEqualTo path >> withMessage "file path"
//                 getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
//                 getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
//             ]
//         )
//     )
//     
// let ``run the test method passed to it when given no tags, no setup, no teardown, no test body indicator`` =
//     feature.Test (
//         Setup setupFeatureUnderTest,
//         TestBody (fun (testFeature: IFeature<unit>) ->
//             let monitor = Monitor<unit, unit, unit> (Ok ())
//             let test =
//                 testFeature.Test (
//                     "My test",
//                     monitor.CallTestActionWithSetupEnvironment,
//                     "D:\\dog.bark",
//                     73
//                 )
//                 
//             test
//             |> silentlyRunTest
//             
//             monitor.TestWasCalled
//             |> Should.BeTrue
//             |> withMessage "test was not called"
//         )
//     )
//
let ``Test Cases`` = feature.GetTests ()