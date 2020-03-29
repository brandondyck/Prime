﻿// Prime - A PRIMitivEs code library.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Prime
open System

[<RequireQualifiedAccess>]
module Constants =

    [<RequireQualifiedAccess>]
    module Address =
    
        let [<Literal>] Separator = '/'
        let [<Literal>] SeparatorStr = "/"

    [<RequireQualifiedAccess>]
    module Relation =
    
        let [<Literal>] Slot = '?'
        let [<Literal>] SlotStr = "?"

    [<RequireQualifiedAccess>]
    module Scripting =
    
        let [<Literal>] ViolationSeparator = '/'
        let [<Literal>] ViolationSeparatorStr = "/"
        let [<Literal>] PreludeFilePath = "Prelude.amsl"

    [<RequireQualifiedAccess>]
    module PrettyPrinter =
    
        let [<Literal>] DefaultThresholdMin = 0
        let [<Literal>] StructuredThresholdMin = 3
        let [<Literal>] SimpleThresholdMax = 1
        let [<Literal>] DefaultThresholdMax = 2
        let [<Literal>] DetailedThresholdMax = 3
        let [<Literal>] CompositionalThresholdMax = 4