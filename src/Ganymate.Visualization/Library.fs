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

    let drawFrame (window:Sdl2.Sdl2Window) sw (gui:ImGuiRenderer) isClicked (cl:CommandList) (gd:GraphicsDevice) elapsedTime =
        let events = window.PumpEvents()
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

        isClicked
        |> Option.map (fun _ -> "IsClicked")
        |> Option.defaultValue "--"
        |> ImGui.Text

        ImGui.Button("test")
        let isJustClicked = ImGui.IsItemClicked ImGuiMouseButton.Left
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
        isJustClicked


    let rec drawFrameOrExit (sw:Stopwatch) isClicked (window:Sdl2.Sdl2Window)  gui cl gd lastFrame =
        if not window.Exists
        then 1
        else
            // actual drawing code
            let currentFrame = sw.ElapsedTicks
            let elapsedTime = currentFrame - lastFrame

            let isJustClicked = drawFrame window sw gui isClicked cl gd elapsedTime
            let isClickedNew =
                match isClicked with
                | Some 0 -> None
                | Some x -> Some (x - 1)
                | None ->
                    if isJustClicked
                    then Some 20_000
                    else None

            drawFrameOrExit sw isClickedNew window gui cl gd currentFrame

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

        let cl = gd.ResourceFactory.CreateCommandList()
        let lastFrame = sw.ElapsedTicks
        let isClicked = Some 10_000
        drawFrameOrExit sw isClicked window gui cl gd lastFrame