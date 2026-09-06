using System.ComponentModel.DataAnnotations;
using NekretnineApp.Models;
using NekretnineApp.Models.ViewModels;

namespace NekretnineApp.Tests;

[TestClass]
public class DomainModelTests
{
    [TestMethod]
    public void PropertyForm_RejectsMissingAndNonPositiveValues()
    {
        var model = new PropertyFormViewModel
        {
            Area = 0,
            Price = 0
        };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);

        Assert.IsFalse(isValid);
        CollectionAssert.IsSubsetOf(
            new[] { nameof(model.Title), nameof(model.Description), nameof(model.CategoryId), nameof(model.Purpose), nameof(model.City), nameof(model.Address), nameof(model.Area), nameof(model.Price) },
            results.SelectMany(result => result.MemberNames).Distinct().ToArray());
    }

    [TestMethod]
    [DataRow(ReservationStatus.Pending, "Na čekanju")]
    [DataRow(ReservationStatus.Approved, "Prihvaćeno")]
    [DataRow(ReservationStatus.Rejected, "Odbijeno")]
    [DataRow(ReservationStatus.Cancelled, "Otkazano")]
    public void ReservationStatus_UsesLocalizedLabels(ReservationStatus status, string expected)
    {
        Assert.AreEqual(expected, status.DisplayName());
    }

    [TestMethod]
    public void PublicRoles_DoNotContainAdministrator()
    {
        CollectionAssert.AreEquivalent(
            new[] { RoleNames.Advertiser, RoleNames.User },
            RoleNames.PublicRoles);
        CollectionAssert.DoesNotContain(RoleNames.PublicRoles, RoleNames.Admin);
    }
}
