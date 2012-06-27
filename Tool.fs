module TuneDraw.Tool

open System
open System.Drawing
open System.Windows.Forms

open TuneDraw.Scene

[<AbstractClass>]
type Tool () =
    abstract member OnMouseDown : MouseEventArgs -> unit
    abstract member OnMouseUp : MouseEventArgs -> unit
    abstract member OnMouseMove : MouseEventArgs -> unit