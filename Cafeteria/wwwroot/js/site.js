// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// mostrar/esconder senha: vale para todo botão com data-mostrar-senha
document.querySelectorAll('[data-mostrar-senha]').forEach(function (botao) {
    botao.addEventListener('click', function () {
        var campo = botao.parentElement.querySelector('input');
        var icone = botao.querySelector('span');

        if (campo.type === 'password') {
            campo.type = 'text';                 // mostra a senha
            icone.className = 'bi bi-eye-slash'; // olho riscado
            botao.setAttribute('aria-label', 'Esconder senha');
        } else {
            campo.type = 'password';             // esconde de novo
            icone.className = 'bi bi-eye';       // olho normal
            botao.setAttribute('aria-label', 'Mostrar senha');
        }
    });
});
