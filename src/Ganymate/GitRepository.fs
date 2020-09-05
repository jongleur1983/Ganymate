namespace Ganymate

open System

open LibGit2Sharp

module GitRepository =
    type FileChangeKind = Added | Modified | Deleted

    type FileChange =
        {
            Path: string
            ChangeKind: FileChangeKind
        }

    type Committer =
        {
            Name: string
            Email: string
            Date: DateTimeOffset
        }

    type Commit =
        {
            Sha: string
            Author: Committer
            Committer: Committer
            MessageShort: string
            Files: FileChange list
            Diff: string
        }

    let isGitRepository directory =
        LibGit2Sharp.Repository.IsValid directory

    let getHead directory =
        use repository = new LibGit2Sharp.Repository(directory)

        repository.Head.FriendlyName
        
    let createCommitter (signature: Signature) =
        {
            Name = signature.Name
            Email = signature.Email
            Date = signature.When
        }

    let readCommits directory =
        use repository = new Repository(directory)

        repository.Head.Commits
        |> Seq.map (fun commit ->
            {
                Sha = commit.Sha
                Author = createCommitter commit.Author
                Committer = createCommitter commit.Committer
                MessageShort = commit.MessageShort
                Files = []
                Diff = ""
            })
        |> Seq.toList

    let getCommits directory =
        use repository = new LibGit2Sharp.Repository(directory)
        repository.Head.Commits |> Seq.toList