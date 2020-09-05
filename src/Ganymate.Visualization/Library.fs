namespace Ganymate.Visualization

open System.Diagnostics
open System.Numerics
open ImGuiNET
open Veldrid
open Veldrid.StartupUtilities

module Program =
    let createWindow =
        let windowCreateInfo =
            WindowCreateInfo(
                X = 400,
                Y = 400,
                WindowHeight = 640,
                WindowWidth = 480,
                WindowTitle = "Ganymate.Visualization"            
            )
        let window = VeldridStartup.CreateWindow windowCreateInfo
        window.add_Closed (fun () -> exit 0 |> ignore )
        window.Resizable <- true
        window.BorderVisible <-true
        window
        
    [<EntryPoint>]
    let main argv =
        let window = createWindow
        let gdo = GraphicsDeviceOptions()
        let gd = VeldridStartup.CreateGraphicsDevice window
        let gui =
            new ImGuiRenderer(
                gd,
                (gd.MainSwapchain.Framebuffer.OutputDescription),
                window.Width,
                window.Height
            )
        let sw = Stopwatch.StartNew()
        let mutable lastFrame = sw.ElapsedMilliseconds 
            
        window.add_Resized (fun () ->
            gd.ResizeMainWindow(uint32 window.Width, uint32 window.Height))

        while window.Exists do
            let cl = gd.ResourceFactory.CreateCommandList()
            let events = window.PumpEvents()
            
            gui.Update(float32 (sw.ElapsedMilliseconds - lastFrame), events)
            ImGui.SetNextWindowSize(Vector2(float32 (window.Width/2), float32 window.Height))
            ImGui.SetNextWindowPos(Vector2(float32 0,float32 0), ImGuiCond.Always)
            ImGui.Begin(
                "main",
                ImGuiWindowFlags.NoMove
                ||| ImGuiWindowFlags.NoCollapse
                ||| ImGuiWindowFlags.NoTitleBar
                ||| ImGuiWindowFlags.NoResize)
            |> ignore

            ImGui.Text "There's text!"
            ImGui.End()            
            
            cl.Begin()
            cl.SetFramebuffer(gd.MainSwapchain.Framebuffer)
            
            if ImGui.GetIO().WantCaptureMouse
            then cl.ClearColorTarget(uint32 0, RgbaFloat.Black)
            else cl.ClearColorTarget(uint32 0, RgbaFloat.CornflowerBlue)
            
            gui.Render (gd, cl)
            cl.End()
            
            gd.SubmitCommands cl
            gd.SwapBuffers()
        0