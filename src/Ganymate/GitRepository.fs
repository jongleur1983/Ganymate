namespace Ganymate

module GitRepository =
    let isGitRepository directory =
        LibGit2Sharp.Repository.IsValid(directory)