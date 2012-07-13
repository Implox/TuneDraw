module TuneDraw.Sound

open System

open OpenTK
open OpenTK.Audio
open OpenTK.Audio.OpenAL

open TuneDraw.Scene

type Sample = float

type SampleBuffer = Sample[]

type StreamInfo (sampleRate : float, channels : int) =
    /// The number of samples per second.
    member this.Rate = sampleRate

    /// The number of data points per sample.
    member this.Channels = channels

[<AbstractClass>]
type SoundGenerator  (streamInfo : StreamInfo) =
    /// Generates the given number of samples and writes them to the given sample buffer at the given offset.
    abstract Write : int -> int -> SampleBuffer -> unit

    member this.StreamInfo = streamInfo

type SineGenerator  (streamInfo : StreamInfo, frequency : float) =
    inherit SoundGenerator  (streamInfo)
    let mutable phase = 0.0
    override this.Write size offset buffer =
        let mutable offset = offset
        for i = 0 to (size - 1) do
            let value = sin (phase * 2.0 * Math.PI)
            for j = 0 to (this.StreamInfo.Channels - 1) do
                buffer.[offset + j] <- value
            offset <- offset + this.StreamInfo.Channels
            phase <- phase + (frequency / this.StreamInfo.Rate)

type SoundPlayer (generator : StreamInfo -> SoundGenerator , sampleRate : int) =
    static let context = new AudioContext ()
    let mutable playing = false
    let sID = AL.GenSource ()
    let streamInfo = StreamInfo (float sampleRate, 1)
    let generator = generator streamInfo
    let bufferSize = 65536 / 2
    let bufferCount = 3
    let sampleBuffer = Array.zeroCreate bufferSize
    let tempBuffer = Array.zeroCreate bufferSize

    /// Writes the next chunk of samples from the generator to the 
    /// OpenAL buffer which corresponds to the given ID and queues it.
    let writeBuffer (bID : int) =
        generator.Write bufferSize 0 sampleBuffer
        for i = 0 to (bufferSize - 1) do 
            tempBuffer.[i] <- int16 (sampleBuffer.[i] * (float Int16.MaxValue))
        AL.BufferData (bID, ALFormat.Mono16, tempBuffer, bufferSize * 2, sampleRate)
        AL.SourceQueueBuffer (sID, bID)

    do for bID in AL.GenBuffers bufferCount do writeBuffer bID

    member this.Update () =
        // Refill processed buffers.
        let mutable buffersProcessed = 0
        AL.GetSource (sID, ALGetSourcei.BuffersProcessed, &buffersProcessed)
        if buffersProcessed > 0 then
            for bID in AL.SourceUnqueueBuffers (sID, buffersProcessed) do writeBuffer bID
            if AL.GetSourceState sID <> ALSourceState.Playing && playing then
                AL.SourcePlay sID

    member this.Play () =
        AL.SourcePlay sID
        playing <- true

    member this.Pause () =
        AL.SourcePause sID
        playing <- false

    /// Gets or sets whether the sound is playing.
    member this.Playing
        with get () = playing
        and set value =
            match playing, value with
            | false, true -> this.Play ()
            | true, false -> this.Pause ()
            | _ -> ()