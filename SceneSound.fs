module TuneDraw.SceneSound

open TuneDraw.Scene
open TuneDraw.Sound

open System

[<AbstractClass>]
type ToneGenerator (sampleRate : float) =

    abstract NextSample : Pitch -> Sample

type OscillatorToneGenerator (sampleRate : float, getWave : float -> Sample) =
    inherit ToneGenerator (sampleRate)
    let mutable phase = 0.0

    override this.NextSample pitch =
        let wave = getWave phase
        let freq = getFrequency pitch
        phase <- (phase + (freq / sampleRate)) % 1.0
        wave

let getSineWave phase = sin (phase * 2.0 * Math.PI)
let getSquareWave phase = if phase < 0.5 then -1.0 else 1.0
let getSawWave phase = phase * 2.0 - 1.0
let getTriWave phase = if phase < 0.5 then 4.0 * phase - 1.0 else 3.0 - 4.0 * phase
let testToneGenerator sampleRate = OscillatorToneGenerator (sampleRate, getTriWave) :> ToneGenerator

/// Creates a playing state for a chain at a given time
let rec playChain (sampleRate : float) (time : Time) (chain : Chain) =
    (chain, testToneGenerator sampleRate) 

let rec updateChains (sampleRate : float) (time : Time) (playing : (Chain * ToneGenerator) list) (remaining : Chain list) = 
        match remaining with
        | ((startPoint, _) as chain) :: tail ->
            if time >= startPoint.Time then
                let playState = playChain sampleRate time chain
                updateChains sampleRate time (playState :: playing) tail
            else (playing, remaining)
        | [] -> (playing, remaining)
    
let rec advanceChain (time : Time) (chain : Chain) =
    let startPoint, segments = chain
    match segments with
    | Line endPoint :: segments ->
        if time < endPoint.Time then
            let lerp = (time - startPoint.Time) / (endPoint.Time - startPoint.Time)
            let pitch : Pitch = (1.0 - lerp) * startPoint.Pitch + lerp * endPoint.Pitch
            Some (pitch, chain)
        else advanceChain time (endPoint, segments)
    | [] -> None

type SceneGenerator (streamInfo : StreamInfo, time : Time, scene : Scene) =
    inherit SoundGenerator (streamInfo)

    let mutable time = time
    let mutable playing = []
    let mutable remaining = List.sortBy (fun (point : Point, _) -> point.Time) scene
    

    member this.Time = time
    override this.Write size offset buffer =
        let mutable offset = offset
        for i = 0 to (size - 1) do
            let nPlaying, nRemaining = updateChains this.StreamInfo.Rate time playing remaining
            playing <- []
            let mutable value = 0.0
            for (chain, tone) in nPlaying do
                match advanceChain time chain with
                | Some (pitch, chain) ->
                    value <- value + tone.NextSample pitch
                    playing <- (chain, tone) :: playing
                | None -> ()

            for j = 0 to (this.StreamInfo.Channels - 1) do
                buffer.[offset + j] <- value * 0.3
            offset <- offset + this.StreamInfo.Channels
            time <- time + (1.0 / this.StreamInfo.Rate)
            remaining <- nRemaining