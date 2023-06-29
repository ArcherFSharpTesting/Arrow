module Archer.Arrows.Tests.Test.Tags.TestFunction.``068 - Feature Test with tags, test function two parameters should``

open System
open Archer
open Archer.Arrows
open Archer.Arrows.Internal.Types
open Archer.Arrows.Tests
open Archer.Arrows.Tests.TestBuilders
open Archer.CoreTypes.InternalTypes
open Archer.MicroLang.Verification

let private feature = Arrow.NewFeature (
    TestTags [
        Category "Feature"
        Category "Test"
    ],
    Setup setupFeatureUnderTest
)

let private rand = Random ()

let private getContainerName (test: ITest) =
    $"%s{test.ContainerPath}.%s{test.ContainerName}"

let ``Create a valid ITest`` =
    feature.Test (fun (_, testFeature: IFeature<string>) ->
        let (_, test), (tags, testName), (path, fileName, lineNumber) =
            TestBuilder.BuildTestWithTagsTestFunctionTwoParameters testFeature

        test
        |> Should.PassAllOf [
            getTags >> Should.BeEqualTo tags >> withMessage "Test Tags"
            getTestName >> Should.BeEqualTo testName >> withMessage "Test Name"
            getFilePath >> Should.BeEqualTo path >> withMessage "File Path"
            getFileName >> Should.BeEqualTo fileName >> withMessage "File Name"
            getLineNumber >> Should.BeEqualTo lineNumber >> withMessage "Line Number"
            getContainerName >> Should.BeEqualTo (testFeature.ToString ()) >> withMessage "Container Name"
        ]
    )

let ``Not call setup when executed`` =
    feature.Test (fun (_, testFeature: IFeature<string>) ->
        let (monitor, test), _, _ = TestBuilder.BuildTestWithTagsTestFunctionTwoParameters testFeature

        test
        |> silentlyRunTest

        monitor.HasSetupFunctionBeenCalled
        |> Should.BeFalse
        |> withFailureComment "Setup was called"
    )

let ``Call Test when executed`` =
    feature.Test (fun (featureSetupValue, testFeature: IFeature<string>) ->
        let (monitor, test), _, _ = TestBuilder.BuildTestWithTagsTestFunctionTwoParameters testFeature

        test
        |> silentlyRunTest

        monitor
        |> Should.PassAllOf [
            hasTestFunctionBeenCalled >> Should.BeTrue >> withFailureComment "Test function not called"

            verifyAllTestFunctionsShouldHaveBeenCalledWithFeatureSetupValueOf featureSetupValue

            verifyNoTestWasCalledWithATestSetupValue
        ]
        |> withMessage "Test was not called"
    )

let ``Not call Test with test environment when executed`` =
    feature.Test (fun (_, testFeature: IFeature<string>) ->
        let (monitor, test), _, _ = TestBuilder.BuildTestWithTagsTestFunctionTwoParameters testFeature

        test
        |> silentlyRunTest

        monitor.HasTestFunctionBeenCalledWithEnvironmentParameter
        |> Should.BeTrue
        |> withFailureComment "Test not called with environment"
    )
    
let ``Call teardown when executed`` =
    feature.Test (fun (_, testFeature: IFeature<string>) ->
        let (monitor, test), _, _ = TestBuilder.BuildTestWithTagsTestFunctionTwoParameters testFeature

        test
        |> silentlyRunTest

        monitor.HasTeardownBeenCalled
        |> Should.BeFalse
        |> withMessage "Teardown was called"
    )

let ``Test Cases`` = feature.GetTests ()