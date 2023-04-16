﻿namespace Archer.Arrow

open System
open Archer

type ApiEnvironment = {
    ApiName: string
    ApiVersion: Version
}

type TestEnvironment = {
    FrameworkEnvironment: FrameworkEnvironment
    ApiEnvironment: ApiEnvironment
    TestInfo: ITestInfo
}

type SetupIndicator<'a> = | Setup of (unit -> Result<'a, SetupTeardownFailure>)
type TestBodyIndicator<'a> = | TestBody of ('a -> TestEnvironment -> TestResult)
type TeardownIndicator<'a> = | Teardown of (Result<'a, SetupTeardownFailure> -> TestResult option -> Result<unit, SetupTeardownFailure>)
type TagsIndicator = | TestTags of TestTag list