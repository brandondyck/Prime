﻿// Prime - A PRIMitivEs code library.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Prime
open System
open Prime

type [<CLIMutable; ReferenceEquality>] EventInfo =
    { ModuleName : string
      FunctionName : string
      MoreInfo : string }

    static member record moduleName functionName =
        { ModuleName = moduleName
          FunctionName = functionName
          MoreInfo = String.Empty }

    static member record3 moduleName functionName moreInfo =
        { ModuleName = moduleName
          FunctionName = functionName
          MoreInfo = moreInfo }

// TODO: P1: consider replacing this with UList since we really want to add to the back anyway.
type EventTrace = EventInfo list

[<RequireQualifiedAccess>]
module EventTrace =

    let record moduleName functionName eventTrace : EventTrace =
        EventInfo.record moduleName functionName :: eventTrace

    let record4 moduleName functionName moreInfo eventTrace : EventTrace =
        EventInfo.record3 moduleName functionName moreInfo :: eventTrace

    let empty : EventTrace =
        []