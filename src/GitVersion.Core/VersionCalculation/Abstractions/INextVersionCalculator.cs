namespace GitVersion.VersionCalculation;

public interface INextVersionCalculator
{
    NextVersion FindVersion();
}
