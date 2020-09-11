module Demo

open System.IO;
open System.Numerics;
open Veldrid;
open Veldrid.Sdl2;
open Veldrid.StartupUtilities;
open Veldrid.SPIRV;
open System.Text;

module Program =
    [<Struct>]
    type VertexPositionColor =
        val Position: Vector2
        val Color: RgbaFloat

        static member SizeInBytes = 24

        new(position: Vector2, color: RgbaFloat) = { Position = position; Color = color }

    type GraphicsResources =
        {
            GraphicsDevice: GraphicsDevice
            CommandList: CommandList
            Pipeline: Pipeline
            VertexBuffer: DeviceBuffer
            IndexBuffer: DeviceBuffer
            Shaders: Shader []
        }

    let vertexCode =
        @"
        #version 450

        layout(location = 0) in vec2 Position;
        layout(location = 1) in vec4 Color;

        layout(location = 0) out vec4 fsin_Color;

        void main()
        {
            gl_Position = vec4(Position, 0, 1);
            fsin_Color = Color;
        }"

    let fragmentCode =
        @"
        #version 450

        layout(location = 0) in vec4 fsin_Color;
        layout(location = 0) out vec4 fsout_Color;

        void main()
        {
            fsout_Color = fsin_Color;
        }"

    let createResources (graphicsDevice: GraphicsDevice) =
        let factory = graphicsDevice.ResourceFactory

        let quadVertices =
            [|
                VertexPositionColor(new Vector2(-0.75f, 0.75f), RgbaFloat.Red)
                VertexPositionColor(new Vector2(0.75f, 0.75f), RgbaFloat.Green)
                VertexPositionColor(new Vector2(-0.75f, -0.75f), RgbaFloat.Blue)
                VertexPositionColor(new Vector2(0.75f, -0.75f), RgbaFloat.Yellow)
            |]

        let vbDescription =
            new BufferDescription(
                4u * uint32 VertexPositionColor.SizeInBytes,
                BufferUsage.VertexBuffer)

        let _vertexBuffer = factory.CreateBuffer(vbDescription);
        graphicsDevice.UpdateBuffer(_vertexBuffer, 0u, quadVertices);

        let quadIndices = [| 0us; 1us; 2us; 3us |]

        let ibDescription =
            new BufferDescription(4u * uint32 sizeof<uint16>, BufferUsage.IndexBuffer);
        let _indexBuffer = factory.CreateBuffer(ibDescription);
        graphicsDevice.UpdateBuffer(_indexBuffer, 0u, quadIndices);

        let vertexLayout =
            VertexLayoutDescription(
                VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

        let vertexShaderDesc =
            ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(vertexCode),
                "main");
        let fragmentShaderDesc =
            ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(fragmentCode),
                "main");

        let _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

        // Create pipeline
        let pipelineDescription =
            GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription(
                    depthTestEnabled=true,
                    depthWriteEnabled=true,
                    comparisonKind=ComparisonKind.LessEqual),
                RasterizerStateDescription(
                    cullMode=FaceCullMode.Back,
                    fillMode=PolygonFillMode.Solid,
                    frontFace=FrontFace.Clockwise,
                    depthClipEnabled=true,
                    scissorTestEnabled=false),
                PrimitiveTopology.TriangleStrip,
                ShaderSetDescription(
                    vertexLayouts=[| vertexLayout |],
                    shaders=_shaders),
                [|  |],
                graphicsDevice.SwapchainFramebuffer.OutputDescription)

        let _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

        let _commandList = factory.CreateCommandList()

        {
            GraphicsDevice = graphicsDevice
            CommandList = _commandList
            Pipeline = _pipeline
            VertexBuffer = _vertexBuffer
            IndexBuffer = _indexBuffer
            Shaders = _shaders
        }

    let draw graphicsResources =
        // Begin() must be called before commands can be issued.
        graphicsResources.CommandList.Begin();

        // We want to render directly to the output window.
        graphicsResources.CommandList.SetFramebuffer(
            graphicsResources.GraphicsDevice.SwapchainFramebuffer);
        graphicsResources.CommandList.ClearColorTarget(0u, RgbaFloat.Black);

        // Set all relevant state to draw our quad.
        graphicsResources.CommandList.SetVertexBuffer(
            0u,
            graphicsResources.VertexBuffer);
        graphicsResources.CommandList.SetIndexBuffer(
            graphicsResources.IndexBuffer,
            IndexFormat.UInt16);
        graphicsResources.CommandList.SetPipeline(graphicsResources.Pipeline);
        // Issue a Draw command for a single instance with 4 indices.
        graphicsResources.CommandList.DrawIndexed(
            indexCount=4u,
            instanceCount=1u,
            indexStart=0u,
            vertexOffset=0,
            instanceStart=0u)

        // End() must be called before commands can be submitted for execution.
        graphicsResources.CommandList.End();
        graphicsResources.GraphicsDevice.SubmitCommands(
            graphicsResources.CommandList);

        // Once commands have been submitted, the rendered image can be presented to the application window.
        graphicsResources.GraphicsDevice.SwapBuffers();

    let disposeResources resources =
        resources.Pipeline.Dispose();

        for shader in resources.Shaders do shader.Dispose()

        resources.CommandList.Dispose();
        resources.VertexBuffer.Dispose();
        resources.IndexBuffer.Dispose();
        resources.GraphicsDevice.Dispose();

    [<EntryPoint>]
    let main argv =
        let windowCI =
            WindowCreateInfo(
                X=100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = "Veldrid Tutorial")

        let window = VeldridStartup.CreateWindow(windowCI);

        let _graphicsDevice = VeldridStartup.CreateGraphicsDevice(window);

        let resources = createResources (_graphicsDevice)

        while window.Exists do
            window.PumpEvents() |> ignore

            if window.Exists then draw resources

        disposeResources resources

        0
