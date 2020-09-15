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

    type Infra =
        {
            window : Sdl2.Sdl2Window;
            gui : ImGuiRenderer;
            cl : CommandList;
            gd : GraphicsDevice;
            sw : Stopwatch;
        }

    type State =
        {
            isClicked : Option<int>
            lastFrame : int64
        }

    let drawFrame (infrastructure:Infra) isClicked elapsedTime =
        let events = infrastructure.window.PumpEvents()
        let frameRate = 1000000.0f / float32 elapsedTime
        infrastructure.gui.Update(float32 elapsedTime, events)
        ImGui.SetNextWindowSize(Vector2(float32 (infrastructure.window.Width), float32 infrastructure.window.Height * 0.25f))
        ImGui.SetNextWindowPos(Vector2(float32 0, float32 infrastructure.window.Height * 0.75f), ImGuiCond.Always)

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

        infrastructure.cl.Begin()
        infrastructure.cl.SetFramebuffer(infrastructure.gd.MainSwapchain.Framebuffer)

        if ImGui.GetIO().WantCaptureMouse
        then infrastructure.cl.ClearColorTarget(uint32 0, RgbaFloat.Black)
        else infrastructure.cl.ClearColorTarget(uint32 0, RgbaFloat.CornflowerBlue)

        infrastructure.gui.Render (infrastructure.gd, infrastructure.cl)
        infrastructure.cl.End()

        infrastructure.gd.SubmitCommands infrastructure.cl
        infrastructure.gd.SwapBuffers()
        isJustClicked


    let rec drawFrameOrExit infrastructure state =
        if not infrastructure.window.Exists
        then 1
        else
            // actual drawing code
            let currentFrame = infrastructure.sw.ElapsedTicks
            let elapsedTime = currentFrame - state.lastFrame

            let isJustClicked = drawFrame infrastructure state.isClicked elapsedTime

            {
                lastFrame = currentFrame
                isClicked =
                    match state.isClicked with
                    | Some 0 -> None
                    | Some x -> Some (x - 1)
                    | None ->
                        if isJustClicked
                        then Some 20_000
                        else None
            }
            |> drawFrameOrExit infrastructure

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

        let cl = gd.ResourceFactory.CreateCommandList()
        let infra =
            {
                Infra.cl = cl
                Infra.gd = gd
                Infra.gui = gui
                Infra.sw = sw
                Infra.window = window
            }
        let state =
            {
                State.lastFrame = sw.ElapsedTicks
                State.isClicked = Some 10_000
            }

        drawFrameOrExit infra state