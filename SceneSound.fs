module TuneDraw.SceneSound

open TuneDraw.Scene
open TuneDraw.Sound

open System

[<AbstractClass>]
type ChainGenerator (sampleRate : float) =
    let mutable finished = false

    member this.Finished
        with get () = finished
        and set value = finished <- value

    abstract NextSample : bool -> Pitch -> Sample

type OscillatorChainGenerator (sampleRate : float, getWave : float -> Sample) =
    inherit ChainGenerator (sampleRate)
    let mutable phase = 0.0
    override this.NextSample chainFinished pitch =
        if chainFinished then
            this.Finished <- true
            0.0
        else
            let wave = getWave phase
            phase <- (phase + (pitch / sampleRate)) % 1.0
            wave

let getSineWave phase = sin (phase * 2.0 * Math.PI)
let getSquareWave phase = if phase > 0.5 then 0.0 else 1.0
let testChainGenerator sampleRate = OscillatorChainGenerator (sampleRate, getSineWave) :> ChainGenerator

/// Creates a playing state for a chain at a given time
let rec playChain (sampleRate : float) (time : Time) (chain : Chain) =
    let startPoint, segments = chain
    match segments with
    | head :: segments ->
        match head with
        | Line endPoint ->
            if time < endPoint.Time then (chain, testChainGenerator sampleRate)
            else playChain sampleRate time (endPoint, segments)
    | [] -> (chain, testChainGenerator sampleRate)
    

type SceneGenerator (streamInfo : StreamInfo, time : Time, scene : Scene) =
    inherit Generator (streamInfo)

    let mutable time = time
    let mutable playingChains = []
    let mutable remainingChains = List.sortBy (fun (point : Point, _) -> point.Time) scene

    member this.Time = time
    override this.Write size offset buffer = ()