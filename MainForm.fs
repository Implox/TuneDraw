module TuneDraw.MainForm

open System
open System.Collections.Generic
open System.Drawing
open System.Windows.Forms

type Line = List<(float * float)>

type MainForm () as this=
    inherit Form ()
    do
        this.Text <- "TuneDraw!"
        this.Width <- 1000
        this.Height <- 511
        this.Cursor <- Cursors.Cross
        this.SetStyle (ControlStyles.AllPaintingInWmPaint, true)
        this.SetStyle (ControlStyles.UserPaint, true)
        this.SetStyle (ControlStyles.OptimizedDoubleBuffer, true)
        this.FormBorderStyle <- FormBorderStyle.FixedSingle
        this.MaximizeBox <- false

    let width () = this.ClientSize.Width
    let height () = this.ClientSize.Height
    let mutable mousePos = Point (0, 0)

    /// The number of grid lines to be displayed.
    let divisions = 50

    let line = Line ()

    /// Finds the distance between a point and the mouse.
    /// Used to find the closest point to the mouse for grid-snapping.
    let dist (x1 : float, y1 : float) =
        let x = float32 (float mousePos.X - x1)
        let y = float32 (float mousePos.Y - y1)
        sqrt (x * x + y * y)
        
    /// List of all the points where the horizontal and vertical gridlines intersect.
    let intersections =
        let width = float (width ())
        let height = float (height ())
        let xDelta = width / float divisions
        let yDelta = width / float divisions
        [for i = 1 to divisions - 1 do 
            for j = 1 to divisions - 1 do
                yield [xDelta * float i; yDelta * float j]]

    /// Paints this form.
    let paint (g : Graphics) =
        g.Clear Color.White

        g.CompositingQuality <- System.Drawing.Drawing2D.CompositingQuality.HighQuality // Fancy drawing because we can!
        g.SmoothingMode <- System.Drawing.Drawing2D.SmoothingMode.HighQuality
        g.TextRenderingHint <- System.Drawing.Text.TextRenderingHint.AntiAlias

        use majorPen = new Pen (Color.Gray, 2.0f)
        use minorPen = new Pen (Color.LightGray, 2.0f)

        let width = float (width ())
        let height = float (height ())
        let xDelta = width / float divisions
        let yDelta = width / float divisions

        for i = 1 to divisions - 1 do
            g.DrawLine (minorPen, float32 xDelta * float32 i, 0.0f, float32 xDelta * float32 i, float32 height)
            g.DrawLine (minorPen, 0.0f, float32 yDelta * float32 i, float32 width, float32 yDelta * float32 i)
        g.DrawLine (majorPen, float32 width / 2.0f, 0.0f, float32 width / 2.0f, float32 height)
        g.DrawLine (majorPen, 0.0f, float32 height / 2.0f, float32 width, float32 height / 2.0f)

        for (x, y) in line do g.FillEllipse (Brushes.Blue, float32 x - 4.0f, float32 y - 4.0f, 8.0f, 8.0f)

        let x, y = [for [a; b] in intersections do yield (a, b)] |> List.minBy dist
        if dist (x, y) <= 10.0f then g.FillEllipse (Brushes.Red, float32 x - 4.0f, float32 y - 4.0f, 8.0f, 8.0f)
        printf "Mouse @: (%A, %A)\n" x y //Just for debugging. Remove later.

    /// Updates this form.
    member this.Update () =
        this.Refresh ()
    
    override this.OnPaint args =
        paint args.Graphics

    override this.OnMouseDown args =
        if args.Button = MouseButtons.Left then
            let x, y = [for [a; b] in intersections do yield (a, b)] |> List.minBy dist
            if dist (x, y) <= 10.0f then
                if line.Contains (x, y) = false then line.Add (x, y)
                else line.Remove(x, y) |> ignore

    override this.OnMouseMove args =
        mousePos <- args.Location
        this.Refresh ()