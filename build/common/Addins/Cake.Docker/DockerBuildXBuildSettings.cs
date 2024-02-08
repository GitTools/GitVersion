#nullable disable
namespace Common.Addins.Cake.Docker;

/// <summary>
/// Settings for docker buildx build.
/// </summary>
public sealed class DockerBuildXBuildSettings : AutoToolSettings
{
    /// <summary>
    /// Add a custom host-to-IP mapping (format: "host:ip")
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] AddHost { get; set; }
    /// <summary>
    /// Allow extra privileged entitlement (e.g., "network.host", "security.insecure")
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Allow { get; set; }
    /// <summary>
    /// Set build-time variables
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] BuildArg { get; set; }
    /// <summary>
    /// Additional build contexts (e.g., name=path)
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] BuildContext { get; set; }
    /// <summary>
    /// Override the configured builder instance
    /// </summary>
    public string Builder { get; set; }
    /// <summary>
    /// External cache sources (e.g., "user/app:cache", "type=local,src=path/to/dir")
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] CacheFrom { get; set; }
    /// <summary>
    /// Cache export destinations (e.g., "user/app:cache", "type=local,dest=path/to/dir")
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] CacheTo { get; set; }
    /// <summary>
    /// Optional parent cgroup for the container
    /// </summary>
    public string CgroupParent { get; set; }
    /// <summary>
    /// Compress the build context using gzip
    /// </summary>
    public bool? Compress { get; set; }
    /// <summary>
    /// Limit the CPU CFS (Completely Fair Scheduler) period
    /// </summary>
    public long? CpuPeriod { get; set; }
    /// <summary>
    /// Limit the CPU CFS (Completely Fair Scheduler) quota
    /// </summary>
    public long? CpuQuota { get; set; }
    /// <summary>
    /// CPUs in which to allow execution (0-3, 0,1)
    /// </summary>
    public string CpusetCpus { get; set; }
    /// <summary>
    /// MEMs in which to allow execution (0-3, 0,1)
    /// </summary>
    public string CpusetMems { get; set; }
    /// <summary>
    /// CPU shares (relative weight)
    /// </summary>
    public long? CpuShares { get; set; }
    /// <summary>
    /// Skip image verification
    /// </summary>
    [AutoProperty(Format = Constants.BoolWithTrueDefaultFormat)]
    public bool? DisableContentTrust { get; set; }
    /// <summary>
    /// Name of the Dockerfile (default: "PATH/Dockerfile")
    /// </summary>
    public string File { get; set; }
    /// <summary>
    /// Always remove intermediate containers
    /// </summary>
    public bool? ForceRm { get; set; }
    /// <summary>
    /// Write the image ID to the file
    /// </summary>
    public string Iidfile { get; set; }
    /// <summary>
    /// Container isolation technology
    /// </summary>
    public string Isolation { get; set; }
    /// <summary>
    /// Set metadata for an image
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Label { get; set; }
    /// <summary>
    /// Shorthand for "--output=type=docker"
    /// </summary>
    public bool Load { get; set; }
    /// <summary>
    /// Memory limit
    /// </summary>
    public string Memory { get; set; }
    /// <summary>
    /// Swap limit equal to memory plus swap: &#39;-1&#39; to enable unlimited swap
    /// </summary>
    public string MemorySwap { get; set; }
    /// <summary>
    /// Write build result metadata to the file
    /// </summary>
    public string MetadataFile { get; set; }
    /// <summary>
    /// Set the networking mode for the "RUN" instructions during build (default "default")
    /// </summary>
    public string Network { get; set; }
    /// <summary>
    /// Do not use cache when building the image
    /// </summary>
    public bool NoCache { get; set; }
    /// <summary>
    /// Do not cache specified stages
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] NoCacheFilter { get; set; }
    /// <summary>
    /// Output destination (format: "type=local,dest=path")
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Output { get; set; }
    /// <summary>
    /// Set target platform for build
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Platform { get; set; }
    /// <summary>
    /// Set type of progress output ("auto", "plain", "tty"). Use plain to show container output (default "auto")
    /// </summary>
    public string Progress { get; set; }
    /// <summary>
    /// Always attempt to pull all referenced images
    /// </summary>
    public bool Pull { get; set; }
    /// <summary>
    /// Shorthand for "--output=type=registry"
    /// </summary>
    public bool Push { get; set; }
    /// <summary>
    /// Suppress the build output and print image ID on success
    /// </summary>
    public bool Quiet { get; set; }
    /// <summary>
    /// Remove intermediate containers after a successful build
    /// </summary>
    [AutoProperty(Format = Constants.BoolWithTrueDefaultFormat)]
    public bool? Rm { get; set; }
    /// <summary>
    /// Secret to expose to the build (format: "id=mysecret[,src=/local/secret]")
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Secret { get; set; }
    /// <summary>
    /// Security options
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] SecurityOpt { get; set; }
    /// <summary>
    /// Size of "/dev/shm"
    /// </summary>
    public string ShmSize { get; set; }
    /// <summary>
    /// Squash newly built layers into a single new layer
    /// </summary>
    public bool? Squash { get; set; }
    /// <summary>
    /// SSH agent socket or keys to expose to the build (format: "default|&lt;id&gt;[=&lt;socket&gt;|&lt;key&gt;[,&lt;key&gt;]]")
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Ssh { get; set; }
    /// <summary>
    /// Name and optionally a tag (format: "name:tag")
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Tag { get; set; }
    /// <summary>
    /// Set the target build stage to build.
    /// </summary>
    public string Target { get; set; }
    /// <summary>
    /// Ulimit options (default [])
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Ulimit { get; set; }
    /// <summary>
    /// Set annotation for new image
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Annotation { get; set; }
}
