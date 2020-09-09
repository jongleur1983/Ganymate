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
            
        window.add_Resized (fun () ->
            gd.ResizeMainWindow(uint32 window.Width, uint32 window.Height))
        let mutable lastFrame = sw.ElapsedTicks
        let mutable isClicked = false

        let cl = gd.ResourceFactory.CreateCommandList()
        while window.Exists do
            let events = window.PumpEvents()
            
            let currentFrame = sw.ElapsedTicks
            let elapsedTime = currentFrame - lastFrame
            let frameRate = 1000000.0f / float32 elapsedTime
            
            gui.Update(float32 elapsedTime, events)
            
            ImGui.SetNextWindowSize(Vector2(float32 (window.Width), float32 window.Height * 0.25f))
            ImGui.SetNextWindowPos(Vector2(float32 0, float32 window.Height * 0.75f), ImGuiCond.Always)

            ImGui.Begin(
                "main",
                ImGuiWindowFlags.NoMove
                ||| ImGuiWindowFlags.NoCollapse
                ||| ImGuiWindowFlags.NoTitleBar
                ||| ImGuiWindowFlags.NoResize)
            |> ignore

            let text = sprintf "FPS %6.2f" frameRate
                
            ImGui.Text(text)

            ImGui.SetNextWindowPos(Vector2(10.f, 20.f))
            ImGui.Separator()
            ImGui.Indent 25.f

            ImGui.Button("test")
            let isClickedNew = ImGui.IsItemClicked ImGuiMouseButton.Left
            isClicked <- isClickedNew || isClicked

            ImGui.Text(isClicked.ToString())

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
            
            lastFrame <- currentFrame
        0