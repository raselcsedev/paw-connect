    [Fact]
    public void ToDto_MarksOnlyTheOwnerAsOwner()
    {
        var owner = Guid.NewGuid();
        var pet = new Pet { Name = "Milo", OwnerId = owner };

        Assert.True(pet.ToDto(owner).IsOwner);
        Assert.False(pet.ToDto(Guid.NewGuid()).IsOwner);
        Assert.False(pet.ToDto(null).IsOwner);
        Assert.False(pet.ToDto().IsOwner);
    }