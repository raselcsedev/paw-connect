    [Fact]
    public void OnlyTheAuthAndAccountEndpoints_HandOutTokens()
    {
        var allowed = new[] { nameof(AuthController), nameof(AccountController) };

        var offenders = Controllers()
            .Where(controller => !allowed.Contains(controller.Name))
            .SelectMany(controller => Actions(controller).Select(action => (controller, action)))
            .Where(pair => Flatten(pair.action.ReturnType).Contains(typeof(AuthResponse)))
            .Select(pair => $"{pair.controller.Name}.{pair.action.Name}")
            .ToList();

        Assert.True(offenders.Count == 0, "These endpoints return tokens: " + string.Join(", ", offenders));
    }