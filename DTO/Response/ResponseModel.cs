namespace API_SVsharp.DTO.Response
{
    // Envelope padrão de todas as respostas da API.
    public class ResponseModel<T>
    {
        public T?     Dados    { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public bool   Status   { get; set; } = true;
    }
}
