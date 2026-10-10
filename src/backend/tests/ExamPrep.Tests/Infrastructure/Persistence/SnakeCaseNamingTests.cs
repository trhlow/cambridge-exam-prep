using ExamPrep.Infrastructure.Persistence;

namespace ExamPrep.Tests.Infrastructure.Persistence;

public sealed class SnakeCaseNamingTests
{
    [Theory]
    [InlineData("Id", "id")]
    [InlineData("ExamSectionId", "exam_section_id")]
    [InlineData("Sha256", "sha256")]
    [InlineData("AudioAdjustMs", "audio_adjust_ms")]
    [InlineData("SebConfigKeyHash", "seb_config_key_hash")]
    [InlineData("HTMLBody", "html_body")]
    [InlineData("ExamTypes", "exam_types")]
    public void ToSnakeCase_ConvertsPascalCaseNames(string input, string expected)
    {
        var actual = SnakeCaseNaming.ToSnakeCase(input);

        Assert.Equal(expected, actual);
    }
}
