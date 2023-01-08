using NUnit.Framework;

namespace Battleship;

public static class Extensions
{
    //todo tdd what if it is null
    public static T AssertSingle<T>(this IEnumerable<T>? collection)
    {
        if (collection is null) throw new Exception("AssertSingle requires non-null target.");
        Assert.That(collection.Count(), Is.EqualTo(1));
        return collection.Single();
    }
}

