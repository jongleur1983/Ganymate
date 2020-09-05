module Tests

open System
open System.IO

open Xunit

open Ganymate
open Xunit.Abstractions

type GitRepositoryTests(testOutputHelper: ITestOutputHelper) = 

    let ensureTestRepositoryExists () =
        let path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Directory.CreateDirectory path
        LibGit2Sharp.Repository.Init(path)
        
        sprintf "Repository exists in '%s'" path
        |> testOutputHelper.WriteLine
        
        path

    [<Fact>]
    let gitRepositoryIsValid () =
        let path = ensureTestRepositoryExists ()
        let actual = GitRepository.isGitRepository path
        Assert.True(actual)

    [<Fact>]
    let gitRepositoryIsNotValid () =
        let path = Environment.GetFolderPath Environment.SpecialFolder.Personal
        let actual = GitRepository.isGitRepository path
        Assert.False(actual)
        
    [<Fact>]
    let emptyGitRepositoryHasNoCommits () =
        let path = ensureTestRepositoryExists ()
        let commits = GitRepository.getCommits path
        Assert.Empty(commits)