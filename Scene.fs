module TuneDraw.Scene

type Time = float

type Pitch = float

type Point (time : float, pitch : float) =
    let mutable t = time
    let mutable p = pitch
    member this.Time
        with get () = t 
        and set value = t <- value

    member this.Pitch 
        with get () = p
        and set value = t <- value

    member this.Location = (t, p)

type Segment =
    | Line of Point

type Chain = (Point * Segment list)

type Scene = Chain list