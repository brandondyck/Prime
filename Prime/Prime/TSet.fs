﻿// Prime - A PRIMitivEs code library.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Prime
open System
open System.Collections.Generic

[<RequireQualifiedAccess>]
module TSet =

    type [<NoEquality; NoComparison>] private 'a Log =
        | Add of add : 'a
        | Remove of remove : 'a
        | Clear

    type [<ReferenceEquality; NoComparison>] 'a TSet =
        private
            { mutable TSetOpt : 'a TSet
              TConfig : TConfig
              HashSet : 'a HashSet
              HashSetOrigin : 'a HashSet
              Logs : 'a Log list
              LogsLength : int }

        static member (>>.) (set : 'a2 TSet, builder : TExpr<unit, 'a2 TSet>) =
            snd' (builder set)

        static member (.>>) (set : 'a2 TSet, builder : TExpr<'a2, 'a2 TSet>) =
            fst' (builder set)

        static member (.>>.) (set : 'a2 TSet, builder : TExpr<'a2, 'a2 TSet>) =
            builder set

    let private commit set =
        let oldSet = set
        let hashSetOrigin = HashSet<'a> (set.HashSetOrigin, set.HashSetOrigin.Comparer)
        List.foldBack (fun log () ->
            match log with
            | Add value -> hashSetOrigin.Add value |> ignore
            | Remove value -> hashSetOrigin.Remove value |> ignore
            | Clear -> hashSetOrigin.Clear ())
            set.Logs ()
        let hashSet = HashSet<'a> (hashSetOrigin, hashSetOrigin.Comparer)
        let set = { set with HashSet = hashSet; HashSetOrigin = hashSetOrigin; Logs = []; LogsLength = 0 }
        oldSet.TSetOpt <- Unchecked.defaultof<'a TSet>
        set.TSetOpt <- set
        set

    let private compress set =
        let oldSet = set
        let hashSetOrigin = HashSet<'a> (set.HashSet, set.HashSet.Comparer)
        let set = { set with HashSetOrigin = hashSetOrigin; Logs = []; LogsLength = 0 }
        oldSet.TSetOpt <- Unchecked.defaultof<'a TSet>
        set.TSetOpt <- set
        set

    let private validate2 set =
        lock set.Logs (fun () ->
            match box set.TSetOpt with
            | null -> commit set
            | target ->
                match obj.ReferenceEquals (target, set) with
                | true -> if set.LogsLength > set.HashSet.Count then compress set else set
                | false -> commit set)

    let private update updater set =
        let oldSet = set
        let set = validate2 set
        let set = updater set
        oldSet.TSetOpt <- Unchecked.defaultof<'a TSet>
        set.TSetOpt <- set
        set

    let private validate set =
        if TConfig.isFunctional set.TConfig
        then validate2 set
        else set

    let makeFromHashSet (comparer : 'a IEqualityComparer) config (hashSet : 'a HashSet) =
        if TConfig.isFunctional config then
            let set =
                { TSetOpt = Unchecked.defaultof<'a TSet>
                  TConfig = config
                  HashSet = hashSet
                  HashSetOrigin = HashSet<'a> (hashSet, comparer)
                  Logs = []
                  LogsLength = 0 }
            set.TSetOpt <- set
            set
        else
            { TSetOpt = Unchecked.defaultof<'a TSet>
              TConfig = config
              HashSet = hashSet
              HashSetOrigin = HashSet<'a> comparer
              Logs = []
              LogsLength = 0 }

    let makeFromSeq<'a> comparer config (items : 'a seq) =
        let hashSet = hashSetPlus comparer items
        makeFromHashSet comparer config hashSet

    let makeEmpty<'a> comparer config =
        makeFromSeq<'a> comparer config Seq.empty

    let getComparer set =
        struct (set.HashSet.Comparer, set)

    let getConfig set =
        struct (set.TConfig, set)

    let add value set =
        if TConfig.isFunctional set.TConfig then
            update (fun set ->
                let set = { set with Logs = Add value :: set.Logs; LogsLength = set.LogsLength + 1 }
                set.HashSet.Add value |> ignore
                set)
                set
        else set.HashSet.Add value |> ignore; set

    let remove value set =
        if TConfig.isFunctional set.TConfig then
            update (fun set ->
                let set = { set with Logs = Remove value :: set.Logs; LogsLength = set.LogsLength + 1 }
                set.HashSet.Remove value |> ignore
                set)
                set
        else set.HashSet.Remove value |> ignore; set

    let clear set =
        if TConfig.isFunctional set.TConfig then
            update (fun set ->
                let set = { set with Logs = Clear :: set.Logs; LogsLength = set.LogsLength + 1 }
                set.HashSet.Clear ()
                set)
                set
        else set.HashSet.Clear (); set

    let isEmpty set =
        let set = validate set
        struct (set.HashSet.Count = 0, set)

    let notEmpty set =
        mapFst' not (isEmpty set)

    /// Get the length of the set (constant-time).
    let length set =
        let set = validate set
        struct (set.HashSet.Count, set)

    let contains value set =
        let set = validate set
        struct (set.HashSet.Contains value, set)

    /// Add all the given values to the set.
    let addMany values set =
        Seq.fold (flip add) set values

    /// Remove all the given values from the set.
    let removeMany values set =
        Seq.fold (flip remove) set values

    /// Convert a TSet to a seq. Note that entire set is iterated eagerly since the underlying HashMap could
    /// otherwise opaquely change during iteration.
    let toSeq set =
        let set = validate set
        let seq = set.HashSet |> Array.ofSeq :> 'a seq
        struct (seq, set)

    /// Convert a TSet to a HashSet.
    let toHashSet set =
        let set = validate set
        let result = HashSet<'a> (set.HashSet, set.HashSet.Comparer)
        struct (result, set)

    let fold folder state set =
        let struct (seq, set) = toSeq set
        let result = Seq.fold folder state seq
        struct (result, set)

    let map mapper set =
        fold
            (fun set value -> add (mapper value) set)
            (makeEmpty set.HashSet.Comparer set.TConfig)
            set

    let filter pred set =
        fold
            (fun set value -> if pred value then add value set else set)
            (makeEmpty set.HashSet.Comparer set.TConfig)
            set

    let equals set set2 =
        let (set, set2) = (validate set, validate set2)
        struct (set.HashSet.SetEquals set2.HashSet, set, set2)

    let unionFast set set2 =
        let (set, set2) = (validate set, validate set2)
        let result = HashSet<'a> (set.HashSet, set.HashSet.Comparer)
        result.UnionWith set2.HashSet
        struct (result, set, set2)

    let intersectFast set set2 =
        let (set, set2) = (validate set, validate set2)
        let result = HashSet<'a> (set.HashSet, set.HashSet.Comparer)
        result.IntersectWith set2.HashSet
        struct (result, set, set2)

    let disjointFast set set2 =
        let (set, set2) = (validate set, validate set2)
        let result = HashSet<'a> (set.HashSet, set.HashSet.Comparer)
        result.SymmetricExceptWith set2.HashSet
        struct (result, set, set2)

    let differenceFast set set2 =
        let (set, set2) = (validate set, validate set2)
        let result = HashSet<'a> (set.HashSet, set.HashSet.Comparer)
        result.ExceptWith set2.HashSet
        struct (result, set, set2)

    let union config set set2 =
        let struct (result, set, set2) = unionFast set set2
        struct (makeFromHashSet set.HashSet.Comparer config result, set, set2)

    let intersect config set set2 =
        let struct (result, set, set2) = intersectFast set set2
        struct (makeFromHashSet set.HashSet.Comparer config result, set, set2)

    let disjoint config set set2 =
        let struct (result, set, set2) = disjointFast set set2
        struct (makeFromHashSet set.HashSet.Comparer config result, set, set2)

    let difference config set set2 =
        let struct (result, set, set2) = differenceFast set set2
        struct (makeFromHashSet set.HashSet.Comparer config result, set, set2)

    let singleton<'a> comparer config (item : 'a) =
        makeFromSeq comparer config [item]

type 'a TSet = 'a TSet.TSet

[<AutoOpen>]
module TSetBuilder =

        let tset<'a> = TExprBuilder<'a TSet> ()