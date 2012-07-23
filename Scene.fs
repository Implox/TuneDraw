module TuneDraw.Scene

type Time = float

type Pitch = float

type Point = Time * Pitch

let getFrequency (pitch : Pitch) = 1000.0 * exp pitch

type Instrument =
    | Sine
    | Saw
    | Triangle
    | Square

/// A mutable point that can be changed manually by the user.
type ControlPoint (time : float, pitch : float) =
    let mutable time = time
    let mutable pitch = pitch

    member this.Time
        with get () = time 
        and set value = time <- value

    member this.Pitch 
        with get () = pitch
        and set value = pitch <- value

    member this.Location = (time, pitch)

/// A point that varies over time.
type PointSignal =
    | Control of ControlPoint
    member this.Location =
        match this with
        | Control controlPoint -> controlPoint.Location

/// A point with a location changes over time.
type DynamicPoint (source : PointSignal) =
    let mutable source = source

    /// Gets or sets the source for the location of this point.
    member this.Source
        with get () = source
        and set value = source <- value

    /// Gets the current location of this dynamic point.
    member this.Location = source.Location

/// A point with a mutable volume and pan and a location that changes over time.
type SoundPoint (volume : float, pan : float, locationSource : PointSignal) =
    inherit DynamicPoint (locationSource)
    let mutable volume = volume
    let mutable pan = pan
    new (volume, pan, time, pitch) = SoundPoint (volume, pan, Control (ControlPoint (time, pitch)))
    new (time, pitch) = SoundPoint (1.0, 0.0, Control (ControlPoint (time, pitch)))

    /// Gets or sets the relative volume of this point, with 0.0 being silence and 1.0 being the loudest
    /// possible sound without clipping.
    member this.Volume
        with get () = volume
        and set value = volume <- value

    /// Gets or sets the pan, or sound direction, for the sound, with -1.0 being left, 0.0 being center and 1.0 being right.
    member this.Pan
        with get () = pan
        and set value = pan <- value

/// Describes a path between two points.
type Segment =
    | Line
    
/// A path through multiple points made up of segments.
type Chain<'a> (head : 'a, tail : (Segment * Chain<'a>) option) =
    let mutable head = head
    let mutable tail = tail
    new (point : 'a) = Chain<'a> (point, None)

    member this.Head
        with get () = head
        and set value = head <- value

    member this.Tail
        with get () = tail
        and set value = tail <- value

    /// Indicates whether this is the end of a chain.
    member this.Terminal = tail.IsNone

    /// Appends a segment and a point to this chain in-place and returns the terminal
    /// chain.
    member this.Append (segment, point) =
        let terminal = Chain<'a> (point, None)
        tail <- Some (segment, terminal)
        terminal

    /// Prepends a point and a segment to this chain and returns the resulting chain.
    member this.Prepend (point, segment) =
        Chain<'a> (point, Some (segment, this))

/// An independent sound in a scene.
type [<ReferenceEquality>] Sound = 
    | Tone of Instrument * Chain<SoundPoint>

type Scene (sounds : Sound list) =
    let mutable sounds = sounds

    member this.Sounds
        with get () = sounds
        and set value = sounds <- value

    member this.InsertSound sound =
        sounds <- sound :: sounds

    member this.RemoveSound sound =
        sounds <- List.filter (fun x -> x <> sound) sounds