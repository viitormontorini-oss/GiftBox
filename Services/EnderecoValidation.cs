using Business;
using Services.Models;
using Services.Validation;
using System.Text.RegularExpressions;

namespace Services;

internal static class EnderecoValidation
{
    public static void Validar(EnderecoCadastro endereco, IValidationDictionary validation, bool exigirCidade)
    {
        if (string.IsNullOrWhiteSpace(endereco.Logradouro) || endereco.Logradouro.Length > 150)
            validation.AddError("Endereco.Logradouro", "Informe a rua (até 150 caracteres).");
        // número da casa: obrigatório, até 10 caracteres (aceita "120A" ou "s/n")
        if (string.IsNullOrWhiteSpace(endereco.Numero) || endereco.Numero.Length > 10)
            validation.AddError("Endereco.Numero", "Informe o número (até 10 caracteres).");
        if (string.IsNullOrWhiteSpace(endereco.Bairro) || endereco.Bairro.Length > 100)
            validation.AddError("Endereco.Bairro", "Informe o bairro (até 100 caracteres).");
        if (exigirCidade && (string.IsNullOrWhiteSpace(endereco.Cidade) || endereco.Cidade.Length > 100))
            validation.AddError("Endereco.Cidade", "Informe a cidade (até 100 caracteres).");
        if (!Regex.IsMatch(endereco.Cep ?? "", @"^\d{5}-?\d{3}$"))
            validation.AddError("Endereco.Cep", "Informe um CEP válido, como 00000-000.");
        if (endereco.Complemento?.Length > 100)
            validation.AddError("Endereco.Complemento", "O complemento deve ter até 100 caracteres.");
    }

    public static Endereco Criar(EnderecoCadastro dados, Cliente cliente, string? cidade = null) => new()
    {
        logradouro = dados.Logradouro!.Trim(), numero = dados.Numero!.Trim(), bairro = dados.Bairro!.Trim(),
        cidade = cidade ?? dados.Cidade!.Trim(), cep = dados.Cep!.Trim(),
        complemento = string.IsNullOrWhiteSpace(dados.Complemento) ? null : dados.Complemento.Trim(),
        Cliente = cliente
    };
}
