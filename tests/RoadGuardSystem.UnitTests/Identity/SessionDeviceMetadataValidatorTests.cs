using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Identity;
using Xunit;

namespace RoadGuardSystem.UnitTests.Identity;

[Trait("TaskId", "P2-10")]
public sealed class SessionDeviceMetadataValidatorTests
{
    [Theory(DisplayName = "P2-10 Negative: Malformed or non-object JSON is rejected")]
    [InlineData("not a json")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("[1, 2, 3]")]
    [InlineData("\"string value\"")]
    [InlineData("12345")]
    [InlineData("true")]
    public void Validate_NonObjectOrMalformedJson_Fails(string invalidJson)
    {
        var result = SessionDeviceMetadataValidator.Validate(invalidJson);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Theory(DisplayName = "P2-10 Negative: Invalid schema version is rejected")]
    [InlineData("{}")]
    [InlineData("{\"schema_version\": 0}")]
    [InlineData("{\"schema_version\": 2}")]
    [InlineData("{\"schema_version\": -1}")]
    [InlineData("{\"schema_version\": \"1\"}")]
    public void Validate_MissingOrInvalidSchemaVersion_Fails(string json)
    {
        var result = SessionDeviceMetadataValidator.Validate(json);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("schema_version");
    }

    [Theory(DisplayName = "P2-10 Negative: Unknown fields are rejected")]
    [InlineData("{\"schema_version\": 1, \"unknown_prop\": \"value\"}")]
    [InlineData("{\"schema_version\": 1, \"ip_address\": \"127.0.0.1\"}")]
    [InlineData("{\"schema_version\": 1, \"location\": {\"lat\": 10, \"lng\": 106}}")]
    public void Validate_UnknownFields_Fails(string json)
    {
        var result = SessionDeviceMetadataValidator.Validate(json);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown field");
    }

    [Theory(DisplayName = "P2-10 Negative: Non-string optional properties are rejected")]
    [InlineData("{\"schema_version\": 1, \"device_id\": 12345}")]
    [InlineData("{\"schema_version\": 1, \"platform\": true}")]
    [InlineData("{\"schema_version\": 1, \"app_version\": [\"1\", \"0\"]}")]
    [InlineData("{\"schema_version\": 1, \"device_id\": {}}")]
    public void Validate_NonStringOptionalFields_Fails(string json)
    {
        var result = SessionDeviceMetadataValidator.Validate(json);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("must be a string");
    }

    [Fact(DisplayName = "P2-10 Negative: Oversize optional fields are rejected")]
    public void Validate_OversizeFields_Fails()
    {
        var options = new SessionDeviceMetadataOptions { MaxDeviceIdLength = 25 };
        var longDeviceId = new string('x', 26);
        var json = $"{{\"schema_version\": 1, \"device_id\": \"{longDeviceId}\"}}";

        var result = SessionDeviceMetadataValidator.Validate(json, options);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("maximum length 25");
    }

    [Fact(DisplayName = "P2-10 Negative: Configured custom boundary limits are enforced")]
    public void Validate_CustomConfiguredBoundaries_Enforced()
    {
        var customOptions = new SessionDeviceMetadataOptions
        {
            MaxDeviceIdLength = 20,
            MaxPlatformLength = 15,
            MaxAppVersionLength = 10,
            SensitiveKeywords = ["custom_forbidden_keyword"]
        };

        // Within custom boundary (length 20)
        var validJson = "{\"schema_version\": 1, \"device_id\": \"12345678901234567890\"}";
        var validResult = SessionDeviceMetadataValidator.Validate(validJson, customOptions);
        validResult.IsValid.Should().BeTrue();

        // Exceeds custom boundary (length 21)
        var invalidJson = "{\"schema_version\": 1, \"device_id\": \"123456789012345678901\"}";
        var invalidResult = SessionDeviceMetadataValidator.Validate(invalidJson, customOptions);
        invalidResult.IsValid.Should().BeFalse();
        invalidResult.ErrorMessage.Should().Contain("exceeds maximum length 20");

        // Custom sensitive keyword check
        var sensitiveOptions = new SessionDeviceMetadataOptions
        {
            MaxDeviceIdLength = 50,
            SensitiveKeywords = ["custom_forbidden"]
        };
        var sensitiveJson = "{\"schema_version\": 1, \"device_id\": \"my_custom_forbidden_id\"}";
        var sensitiveResult = SessionDeviceMetadataValidator.Validate(sensitiveJson, sensitiveOptions);
        sensitiveResult.IsValid.Should().BeFalse();
        sensitiveResult.ErrorMessage.Should().Contain("sensitive");
    }

    [Theory(DisplayName = "P2-10 Negative: Secret or token content is rejected")]
    [InlineData("{\"schema_version\": 1, \"device_id\": \"password123\"}")]
    [InlineData("{\"schema_version\": 1, \"platform\": \"my-secret-token\"}")]
    [InlineData("{\"schema_version\": 1, \"app_version\": \"bearer eyJhbGci...\"}")]
    public void Validate_SecretContent_Fails(string json)
    {
        var result = SessionDeviceMetadataValidator.Validate(json);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("sensitive");
    }

    [Fact(DisplayName = "P2-10 Positive: Null metadata is valid")]
    public void Validate_NullMetadata_Passes()
    {
        var result = SessionDeviceMetadataValidator.Validate(null);
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "P2-10 Positive: Minimal valid schema_version 1 passes")]
    public void Validate_MinimalValidMetadata_Passes()
    {
        var json = "{\"schema_version\": 1}";
        var result = SessionDeviceMetadataValidator.Validate(json);
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "P2-10 Positive: Complete valid metadata passes")]
    public void Validate_CompleteValidMetadata_Passes()
    {
        var json = "{\"schema_version\": 1, \"device_id\": \"pixel-7-pro\", \"platform\": \"Android 14\", \"app_version\": \"1.0.0\"}";
        var result = SessionDeviceMetadataValidator.Validate(json);
        result.IsValid.Should().BeTrue();
    }
}
