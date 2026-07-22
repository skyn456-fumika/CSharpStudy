using System.ComponentModel.DataAnnotations;

namespace MemoBlazor.Models;

public class MemoCreateRequest
{
    [Required(ErrorMessage = "제목은 필수입니다.")]
    [StringLength(100, ErrorMessage = "제목은 100자 이하로 입력해야 합니다.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "내용은 필수입니다.")]
    [StringLength(2000, ErrorMessage = "내용은 2000자 이하로 입력해야 합니다.")]
    public string Content { get; set; } = "";

    [Required(ErrorMessage = "카테고리는 필수입니다.")]
    [StringLength(30, ErrorMessage = "카테고리는 30자 이하로 입력해야 합니다.")]
    public string Category { get; set; } = "일반";
}