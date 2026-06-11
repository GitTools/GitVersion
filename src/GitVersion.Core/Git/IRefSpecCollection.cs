namespace GitVersion.Git;

/// <summary>Represents an ordered collection of <see cref="IRefSpec"/> objects for a remote.</summary>
public interface IRefSpecCollection : IEnumerable<IRefSpec>;
