module TuneDraw.Program

open System
open System.Threading
open System.Windows.Forms

open MainForm

[<EntryPoint>]
let main argv = 
    let form = new MainForm ()
    form.Show ()
    form.Activate ()
    while form.Visible do
        form.Update ()
        Application.DoEvents ()
    0