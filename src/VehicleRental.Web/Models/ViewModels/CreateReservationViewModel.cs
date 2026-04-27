using System.ComponentModel.DataAnnotations;

namespace VehicleRental.Web.Models.ViewModels;

public class CreateReservationViewModel
{
    public int VehicleTypeId { get; set; }
    public string VehicleTypeName { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }

    [Required(ErrorMessage = "請選擇起始日期")]
    [DataType(DataType.Date)]
    [Display(Name = "租用起始日")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "請選擇結束日期")]
    [DataType(DataType.Date)]
    [Display(Name = "租用結束日")]
    public DateOnly EndDate { get; set; }

    public decimal? EstimatedCost { get; set; }
}
