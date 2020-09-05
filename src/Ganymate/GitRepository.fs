namespace Ganymate

module GitRepository =
    let isGitRepository directory =
        LibGit2Sharp.Repository.IsValid directory

    let getHead directory =
        use repository = new LibGit2Sharp.Repository(directory)

        repository.Head.FriendlyName
        
    let getCommits directory =
        use repository = new LibGit2Sharp.Repository(directory)
        repository.Head.Commits |> Seq.toList