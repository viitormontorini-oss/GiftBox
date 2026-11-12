using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Validation;

namespace Cafeteria.Areas.Admin.Controllers;

// base de todos os controllers do gestor (igual ao projeto do Yago):
// Area "Admin", só com login, e todo POST precisa do token do formulário
[Area("Admin")]
[Authorize]
[AutoValidateAntiforgeryToken]
public abstract class AdminBaseController : Controller
{
    // tela de confirmação comum (usada pelos Usuários; pedidos, produtos e clientes têm a tela de excluir própria)
    protected IActionResult ConfirmarExclusao(int id, string descricao)
    {
        ViewData["Id"] = id;
        ViewData["Descricao"] = descricao;
        return View("~/Areas/Admin/Views/Shared/Excluir.cshtml");
    }

    // erros da camada Services aparecem na tela (asp-validation-summary)
    protected void CopiarValidacoes(IValidationDictionary validation)
    {
        foreach (var erro in validation.Errors)
            foreach (var mensagem in erro.Value)
                ModelState.AddModelError(erro.Key, mensagem);
    }
}
