# GiftBox

Aplicação ASP.NET Core MVC (.NET 10) com MySQL.

## Publicação no Render

Este projeto precisa de **Web Service / Docker**, não Static Site.
O `render.yaml` configura o plano gratuito; o Render compila o `Dockerfile`.

1. Crie um banco MySQL online vazio. Uma alternativa com cota gratuita é TiDB Cloud Starter, no modo compatível com MySQL, sem ativar cobrança. A compatibilidade da inicialização precisa ser validada no banco escolhido.
2. No Render, crie um Blueprint a partir deste repositório (ou Web Service com runtime Docker e plano Free).
3. Configure `ConnectionStrings__DefaultConnection` de forma privada no Render:
   `Server=HOST;Port=4000;Database=DBGiftBox;User=USUARIO;Password=SENHA;SslMode=VerifyFull;`
   Use host, porta, banco e usuário fornecidos pelo provedor. Nunca coloque a conexão real no GitHub.
4. O Blueprint gera `BootstrapAdmin__Password`. Na configuração manual, defina uma senha aleatória com pelo menos 12 caracteres. O primeiro administrador usa o e-mail `admin@giftbox.local` e login `admin`. A senha só é aplicada se ainda não existir nenhum usuário.
5. Publique e verifique `/health`, o catálogo e o login. O banco precisa estar acessível e inicializar corretamente para a aplicação iniciar em produção.

## Limitações ainda pendentes

- Banco online e serviço Render precisam ser criados e conectados às contas do proprietário.
- A pasta de origem não contém um dump do banco local. A primeira execução cria o esquema e o catálogo inicial do código; não importa clientes ou pedidos existentes.
- As imagens incluídas no Git permanecem disponíveis. Novas fotos enviadas pelo painel são gravadas no disco temporário do Render Free e se perdem em reinícios/deploys. Configure armazenamento externo antes de usar uploads permanentemente.
- Instâncias gratuitas do Render podem suspender por inatividade. Consulte os limites atuais dos provedores.
- Não foi executado teste de integração contra o banco online nem build Linux no Docker local.

## Desenvolvimento

Instale o SDK .NET 10 e use um MySQL de desenvolvimento. Configure a conexão e a senha inicial por variáveis de ambiente com os mesmos nomes acima.

```sh
dotnet restore Cafeteria.sln
dotnet run --project Cafeteria/Cafeteria.csproj
```

Build de publicação:

```sh
dotnet publish Cafeteria/Cafeteria.csproj -c Release -o publish
```
