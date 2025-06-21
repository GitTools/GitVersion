using GitVersion.Testing.Helpers;

namespace GitVersion.Core.Tests.Helpers;

[TestFixture]
public class ParticipantSanitizerTests
{
    [TestCase("feature/1234-is-id-with-something-kebab", "feature_1234_is_id_with_something_kebab")]
    [TestCase("feature/1234-IsSomethingPascalCase", "feature_1234_IsSomethingPascalCase")]
    [TestCase("feature/Caps-lower-something-kebab", "feature_Caps_lower_something_kebab")]
    [TestCase("feature/Caps-lower-is-kebab", "feature_Caps_lower_is_kebab")]
    [TestCase("kebab-folder/1234-is-id-with-something-kebab", "kebab_folder_1234_is_id_with_something_kebab")]
    [TestCase("kebab-folder/1234-IsSomethingPascalCase", "kebab_folder_1234_IsSomethingPascalCase")]
    [TestCase("kebab-folder/Caps-lower-something-kebab", "kebab_folder_Caps_lower_something_kebab")]
    [TestCase("kebab-folder/Caps-lower-is-kebab", "kebab_folder_Caps_lower_is_kebab")]
    [TestCase("PascalCaseFolder/1234-is-id-with-something-kebab", "PascalCaseFolder_1234_is_id_with_something_kebab")]
    [TestCase("PascalCaseFolder/1234-IsSomethingPascalCase", "PascalCaseFolder_1234_IsSomethingPascalCase")]
    [TestCase("PascalCaseFolder/Caps-lower-something-kebab", "PascalCaseFolder_Caps_lower_something_kebab")]
    [TestCase("PascalCaseFolder/Caps-lower-is-kebab", "PascalCaseFolder_Caps_lower_is_kebab")]
    [TestCase("1234-is-id-with-something-kebab", "1234_is_id_with_something_kebab")]
    [TestCase("1234-IsSomethingPascalCase", "1234_IsSomethingPascalCase")]
    [TestCase("Caps-lower-something-kebab", "Caps_lower_something_kebab")]
    [TestCase("Caps-lower-is-kebab", "Caps_lower_is_kebab")]
    [TestCase("feature/all-lower-is-kebab", "feature_all_lower_is_kebab")]
    [TestCase("feature/24321-Upperjustoneword", "feature_24321_Upperjustoneword")]
    [TestCase("feature/justoneword", "feature_justoneword")]
    [TestCase("feature/PascalCase", "feature_PascalCase")]
    [TestCase("feature/PascalCase-with-kebab", "feature_PascalCase_with_kebab")]
    [TestCase("feature/12414", "feature_12414")]
    [TestCase("feature/12414/12342-FeatureStoryTaskWithShortDescription", "feature_12414_12342_FeatureStoryTaskWithShortDescription")]
    [TestCase("feature/12414/12342-Short-description", "feature_12414_12342_Short_description")]
    [TestCase("feature/12414/12342-short-description", "feature_12414_12342_short_description")]
    [TestCase("feature/12414/12342-Short-Description", "feature_12414_12342_Short_Description")]
    [TestCase("release/1.0.0", "release_1_0_0")]
    [TestCase("releases", "releases")]
    [TestCase("feature", "feature")]
    [TestCase("feature/tfs1-Short-description", "feature_tfs1_Short_description")]
    [TestCase("feature/f2-Short-description", "feature_f2_Short_description")]
    [TestCase("feature/bug1", "feature_bug1")]
    [TestCase("f2", "f2")]
    [TestCase("feature/f2", "feature_f2")]
    [TestCase("feature/story2", "feature_story2")]
    [TestCase("master", "master")]
    [TestCase("develop", "develop")]
    [TestCase("main", "main")]
    public void SanitizeValidParticipant_ShouldReturnExpectedResult(string input, string expected)
    {
        var actual = ParticipantSanitizer.SanitizeParticipant(input);
        actual.ShouldBe(expected);
    }

    [TestCase("")]
    [TestCase(" ")]
    public void SanitizeEmptyOrWhitespaceParticipant_ShouldThrow(string value)
    {
        var exception = Should.Throw<ArgumentException>(() => ParticipantSanitizer.SanitizeParticipant(value));
        exception.Message.ShouldBe("The value cannot be an empty string or composed entirely of whitespace. (Parameter 'participant')");
    }

    [TestCase("feature/")]
    [TestCase("/")]
    public void SanitizeInvalidParticipant_ShouldThrow(string value)
    {
        var exception = Should.Throw<ArgumentException>(() => ParticipantSanitizer.SanitizeParticipant(value));
        exception.Message.ShouldBe("The value cannot end with a folder separator ('/'). (Parameter 'participant')");
    }
}
