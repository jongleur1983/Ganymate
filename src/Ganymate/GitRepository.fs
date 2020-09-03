namespace Ganymate

module GitRepository =
    let isGitRepository directory =
        LibGit2Sharp.Repository.IsValid directory

    let getHead directory =
        use repository = new LibGit2Sharp.Repository(directory)

        repository.Head.Tip.Sha