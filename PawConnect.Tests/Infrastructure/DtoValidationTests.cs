    [Theory]
    [InlineData("short")]
    [InlineData("123456789")]
    public void ChangePassword_ShortNewPassword_IsRejected(string password)
    {
        var request = new ChangePasswordRequest { CurrentPassword = "Old#Password2026", NewPassword = password };

        Assert.True(Invalid(request, nameof(ChangePasswordRequest.NewPassword)));
    }

    [Fact]
    public void ChangePassword_MissingCurrentPassword_IsRejected()
    {
        var request = new ChangePasswordRequest { NewPassword = "Brand#New2026Pass" };

        Assert.True(Invalid(request, nameof(ChangePasswordRequest.CurrentPassword)));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("A", true)]
    [InlineData("Sam", false)]
    public void UpdateProfile_DisplayNameNeedsAtLeastTwoCharacters(string name, bool invalid)
    {
        Assert.Equal(invalid, Invalid(new UpdateProfileRequest { DisplayName = name }, nameof(UpdateProfileRequest.DisplayName)));
    }