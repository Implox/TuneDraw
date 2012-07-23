module TuneDraw.MainForm

open System
open System.Collections.Generic
open System.Drawing
open System.Windows.Forms

open TuneDraw.Scene
open TuneDraw.Tool
open TuneDraw.Sound
open TuneDraw.SceneSound

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
    let mutable mousePos = Drawing.Point (0, 0)
    let mutable leftDown = false
    let divisions = 50

    let testChain : Chain = (Scene.Point (0.0, 0.0), [Line (Scene.Point (1.0, -0.3)); Line(Scene.Point (2.0, -0.3)); Line (Scene.Point (4.0, 0.0))])
    let testChainTwo : Chain = (Scene.Point (0.0, 0.1), [Line (Scene.Point (1.0, -0.2)); Line (Scene.Point (2.0, 0.3)); Line (Scene.Point (4.0, 0.0))])
    let testScene : Scene = [testChain; testChainTwo]
    let soundPlayer = Sound.SoundPlayer ((fun streamInfo -> SceneGenerator (streamInfo, 0.0, testScene) :> SoundGenerator), 44100)

    do soundPlayer.Play ()

    let windowToScene (p : PointF) = 
        let height = height ()
        let time = p.X / 200.0f
        let pitch = 0.5f - (p.Y / float32 height) 
        (float time, float pitch)

    let sceneToWindow (time : Time, pitch : Pitch) =
        let height = height ()
        let x = 200.0 * time
        let y = (0.5 - pitch) * float height
        PointF (float32 x, float32 y)

    /// Paints this form.
    let paint (g : Graphics) =
        g.Clear Color.White
        g.CompositingQuality <- System.Drawing.Drawing2D.CompositingQuality.HighQuality // Fancy drawing because we can!
        g.SmoothingMode <- System.Drawing.Drawing2D.SmoothingMode.HighQuality
        g.TextRenderingHint <- System.Drawing.Text.TextRenderingHint.AntiAlias

        use majorPen = new Pen (Color.Red, 2.0f)
        use minorPen = new Pen (Color.LightGray, 0.1f)
        use linePen = new Pen (Color.Blue, 5.0f)

        let width = float (width ())
        let height = float (height ())
        let xDelta = width / float divisions
        let yDelta = width / float divisions

        for i = 1 to divisions - 1 do
            g.DrawLine (minorPen, float32 xDelta * float32 i, 0.0f, float32 xDelta * float32 i, float32 height)
            g.DrawLine (minorPen, 0.0f, float32 yDelta * float32 i, float32 width, float32 yDelta * float32 i)

        g.DrawLine (majorPen, float32 width / 2.0f, 0.0f, float32 width / 2.0f, float32 height)
        g.DrawLine (majorPen, 0.0f, float32 height / 2.0f, float32 width, float32 height / 2.0f)

        for chain in testScene do
            let startPoint, segments = chain
            let mutable startPoint = startPoint
            for segment in segments do
                match segment with
                | Line endPoint ->
                    let s = sceneToWindow startPoint.Location
                    let e = sceneToWindow endPoint.Location
                    g.DrawLine (linePen, s, e)
                    startPoint <- endPoint

    /// Updates this form.
    member this.Update () =
        soundPlayer.Update ()
        this.Refresh ()
    
    override this.OnPaint args =
        paint args.Graphics

    override this.OnMouseDown args = ()

    override this.OnMouseMove args =
        mousePos <- args.Location
        if args.Button = MouseButtons.Left then leftDown <- true
        else leftDown <- false
        this.Refresh ()