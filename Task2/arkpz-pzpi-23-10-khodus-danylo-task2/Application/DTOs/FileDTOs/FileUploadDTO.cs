namespace Application.DTOs.FileDTOs
{
    public class FileUploadDTO
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Length { get; set; }
        public Stream Content { get; set; } = Stream.Null;
    }
}
