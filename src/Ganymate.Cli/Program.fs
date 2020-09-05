namespace Ganymate.Cli

open System
open System.IO

open Argu

open Ganymate

[<AutoOpen>]
module Prelude =
    let traceColored color (s : string) =
        let curColor = Console.ForegroundColor
        if curColor <> color then Console.ForegroundColor <- color
        use textWriter =
            match color with
            | ConsoleColor.Red -> Console.Error
            | ConsoleColor.Yellow -> Console.Out
            | _ -> Console.Out

        textWriter.WriteLine s
        if curColor <> color then Console.ForegroundColor <- curColor

[<AutoOpen>]
module CliHandling =
    type CommandLineParameters =
        {
            IsValid: bool
            Directory: string
            IsVerbose: bool
            ShowHelp: bool
        } with
            static member Empty =
                {
                    IsValid = false
                    Directory = ""
                    IsVerbose = false
                    ShowHelp = false
                }

    type GanymateExiter() =
        interface IExiter with
            member __.Name = "Ganymate exiter"
            member __.Exit (msg, code) =
                if code = ErrorCode.HelpText then
                    printfn "%s" msg ; exit 0
                else traceColored ConsoleColor.Red msg ; exit 1

    [<CliPrefix(CliPrefix.DoubleDash)>]
    [<NoAppSettings>]
    type CliArgument =
        | [<MainCommand>] Directory of string
        | [<AltCommandLine("-v")>] Verbose
        with
            interface IArgParserTemplate with
                member arg.Usage =
                    match arg with
                    | Directory _ -> "The directory containing the Git repository"
                    | Verbose -> "Show additional output for debugging and similar"

    let rec collectCommandLineParameters remainingArguments parameters =
        match remainingArguments with
        | head :: tail ->
            let parameters =
                match head with
                | Directory directory -> { parameters with Directory = directory }
                | Verbose -> { parameters with IsVerbose = true }

            collectCommandLineParameters tail parameters
        | [] ->
            match parameters.Directory with
            | "" | "." ->
                { parameters with Directory = Directory.GetCurrentDirectory() }
            | _ -> parameters

    let processCommandLineArguments (parser: ArgumentParser<CliArgument>) argv =
        let results = parser.ParseCommandLine argv
        let cliArguments = results.GetAllResults()

        if results.IsUsageRequested
        then { CommandLineParameters.Empty with ShowHelp = true }
        else
            CommandLineParameters.Empty
            |> collectCommandLineParameters cliArguments

module Program =
    let printVersion () =
        let productName, version =
            let assembly = System.Reflection.Assembly.GetExecutingAssembly()

            let fileVersionInfo =
                System.Diagnostics.FileVersionInfo.GetVersionInfo assembly.Location

            fileVersionInfo.ProductName, assembly.GetName().Version

        printfn "%s v%i.%i.%i" productName version.Major version.Minor version.Build

    type ExitCode =
        | Ok = 0
        | NoDirectory = 1
        | NoRepository = 2

    let (|Repository|Directory|NoDirectory|) directory =
        if Directory.Exists directory
        then
            if GitRepository.isGitRepository directory
            then Repository
            else Directory
        else NoDirectory

    let getRepositoryInfo directory =
        [
            sprintf "Showing commits in %s" directory
            sprintf "HEAD is %s" (GitRepository.getHead directory)

            ""

            yield!
                directory
                |> GitRepository.readCommits
                |> Seq.map (fun commit ->
                    sprintf "%s %s %s %s" commit.Sha.[0..7] (commit.Committer.Date.ToString "s") commit.Author.Name commit.MessageShort)
        ]

    [<EntryPoint>]
    let main argv =
        let parser =
            ArgumentParser.Create<CliArgument>(
                programName = "Ganymate",
                helpTextMessage = "Help requested",
                errorHandler = GanymateExiter())
        let parameters = processCommandLineArguments parser argv

        if parameters.IsVerbose then printfn "Got parse results %A" ()

        let exitCode, output =
            if parameters.ShowHelp
            then ExitCode.Ok, [ parser.PrintUsage() ]
            else
                match parameters.Directory with
                | Repository ->
                    ExitCode.Ok, getRepositoryInfo parameters.Directory
                | Directory ->
                    ExitCode.NoRepository, [ sprintf "Directory %s is not a Git repository" parameters.Directory ]
                | NoDirectory ->
                    ExitCode.NoDirectory, [ sprintf "Directory %s is not a directory" parameters.Directory ]

        output |> List.iter (printfn "%s")

        int exitCode
