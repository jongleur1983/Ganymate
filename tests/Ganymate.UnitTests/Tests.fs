module Tests

open Xunit

[<Fact>]
let xunitTest () =
    let actual = "Hello Ganymate"

    let expected = "Hello Ganymate"

    Assert.Equal(expected, actual)
