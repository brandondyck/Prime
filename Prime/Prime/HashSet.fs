﻿// Prime - A PRIMitivEs code library.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Prime
open System.Collections.Generic

[<RequireQualifiedAccess>]
module HashSet =

    /// Make a hash set with a single item.
    let inline singleton comparer item =
        List.toHashSet comparer [item]
        
    /// Hash a hash set.
    let hash (hashSet : _ HashSet) =
        let mutable h = 0
        for item in hashSet do
            h <- h ^^^ item.GetHashCode ()
        h

[<AutoOpen>]
module HashSetOperators =

    /// Make a concrete HashSet instance populated with the given items and using vanilla hashing.
    let hashSet<'a> items =
        let hashSet = HashSet ()
        for item in items do hashSet.Add item |> ignore
        hashSet

    /// Make a concrete HashSet instance populated with the given items and using structural hashing.
    let hashSetPlus<'a> (comparer : 'a IEqualityComparer) items =
        let hashSet = HashSet comparer
        for item in items do hashSet.Add item |> ignore
        hashSet