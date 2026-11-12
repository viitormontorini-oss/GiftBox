using Business;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Repository.EF;

public class DBinitializer
{
    public static void initialize(Context context)
    {
        // banco novo: cria tudo, coloca as cestas e termina aqui
        if (context.Database.EnsureCreated())
        {
            CriarCestas(context);
            CriarFotosExtras(context);  // banco novo: leva as fotos de ângulo para a tabela ProdutoImagens
            return;
        }

        // Atualização pontual do esquema anterior, sem excluir tabelas ou cadastros.
        // Valores históricos não registrados permanecem nulos; não se inventam preços.
        var connection = context.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) connection.Open();
        bool tabelaFotosNova = false;  // vira true quando a tabela ProdutoImagens for criada agora
        try
        {
            // a tabela de fotos extras ainda não existe? então ela é criada agora e recebe as fotos de ângulo
            tabelaFotosNova = !TableExists(connection, "ProdutoImagens");
            // cria a tabela das fotos extras: id, número do produto e caminho da foto
            Execute(connection, """
                CREATE TABLE IF NOT EXISTS `ProdutoImagens` (
                    `id` int NOT NULL AUTO_INCREMENT,
                    `produtoId` int NOT NULL,
                    `arquivo` varchar(255) NOT NULL,
                    PRIMARY KEY (`id`),
                    KEY `IX_ProdutoImagens_produtoId` (`produtoId`),
                    CONSTRAINT `FK_ProdutoImagens_produtos_produtoId` FOREIGN KEY (`produtoId`) REFERENCES `produtos` (`id`)
                )
                """);
            Execute(connection, """
                CREATE TABLE IF NOT EXISTS `CidadesEntrega` (
                    `id` int NOT NULL AUTO_INCREMENT,
                    `nome` varchar(100) NOT NULL,
                    `taxa` decimal(12,2) NOT NULL,
                    `ativa` tinyint(1) NOT NULL,
                    PRIMARY KEY (`id`),
                    UNIQUE KEY `IX_CidadesEntrega_nome` (`nome`)
                )
                """);
            // cria a tabela de categorias, se ainda não existir
            Execute(connection, """
                CREATE TABLE IF NOT EXISTS `Categorias` (
                    `id` int NOT NULL AUTO_INCREMENT,
                    `nome` varchar(100) NOT NULL,
                    `imagem` varchar(255) NULL,
                    PRIMARY KEY (`id`)
                )
                """);
            // acrescenta em produtos a coluna com o número da categoria, se ainda não existir
            if (ColumnNullable(connection, "produtos", "categoriaId") == null)
                Execute(connection, "ALTER TABLE `produtos` ADD COLUMN `categoriaId` int NULL, ADD CONSTRAINT `FK_produtos_Categorias` FOREIGN KEY (`categoriaId`) REFERENCES `Categorias` (`id`)");
            // número da casa separado da rua
            if (ColumnNullable(connection, "Enderecos", "numero") == null)
                Execute(connection, "ALTER TABLE `Enderecos` ADD COLUMN `numero` varchar(10) NULL");
            // nome de quem recebe a cesta
            if (ColumnNullable(connection, "pedidos", "nome_recebe") == null)
                Execute(connection, "ALTER TABLE `pedidos` ADD COLUMN `nome_recebe` varchar(60) NULL");
            // pedido feito pelo site não tem funcionário: a coluna passa a aceitar vazio
            if (ColumnNullable(connection, "pedidos", "usuarioid") == "NO")
                Execute(connection, "ALTER TABLE `pedidos` MODIFY COLUMN `usuarioid` int NULL");
            if (ColumnNullable(connection, "pedidos", "taxa_entrega") == null)
                Execute(connection, "ALTER TABLE `pedidos` ADD COLUMN `taxa_entrega` decimal(12,2) NULL");
            if (ColumnNullable(connection, "pedidos", "cidade_entrega_id") == null)
                Execute(connection, "ALTER TABLE `pedidos` ADD COLUMN `cidade_entrega_id` int NULL");
            if (ColumnNullable(connection, "produtos", "imagem") == null && TableExists(connection, "produtos"))
                Execute(connection, "ALTER TABLE `produtos` ADD COLUMN `imagem` varchar(255) NULL");
            if (ColumnNullable(connection, "ProdutoPedidos", "preco_unitario") == null)
                Execute(connection, "ALTER TABLE `ProdutoPedidos` ADD COLUMN `preco_unitario` decimal(12,2) NULL");
            if (ColumnNullable(connection, "ProdutoPedidos", "id") == null)
                Execute(connection, "ALTER TABLE `ProdutoPedidos` ADD COLUMN `id` int NOT NULL AUTO_INCREMENT, ADD INDEX `IX_ProdutoPedidos_pedidoId` (`pedidoId`), DROP PRIMARY KEY, ADD PRIMARY KEY (`id`)");
            if (ColumnNullable(connection, "pedidos", "enderecoId") == "NO")
                Execute(connection, "ALTER TABLE `pedidos` MODIFY COLUMN `enderecoId` int NULL");
            if (ColumnNullable(connection, "Enderecos", "complemento") == "NO")
                Execute(connection, "ALTER TABLE `Enderecos` MODIFY COLUMN `complemento` varchar(100) NULL");
        }
        finally { if (openedHere) connection.Close(); }

        // coloca as categorias e as cestas, se ainda não tiver
        CriarCestas(context);
        // só na primeira vez: leva as fotos de ângulo para a tabela nova (se o gestor apagar depois, não voltam)
        if (tabelaFotosNova)
        {
            CriarFotosExtras(context);
        }
    }

    // Leva as fotos de ângulo para a tabela ProdutoImagens.
    // As fotos estão em wwwroot/imgs/produtos/angulos/<nome da foto principal>/1.jpg, 2.jpg e 3.jpg.
    private static void CriarFotosExtras(Context context)
    {
        // nomes das pastas de ângulos (iguais ao nome do arquivo da foto principal)
        string[] comAngulos =
        {
            "aniversario_cerquinha_urso", "aniversario_golfinho", "aniversario_parabens",
            "balao_gratidao", "maes_balao_frios", "maes_balao_rosa", "maes_balao_roxo",
            "maes_flores_caneca", "maes_orquidea", "namorados_bandeja_eu_te_amo",
            "namorados_coracao", "namorados_eu_te_amo", "namorados_love", "namorados_urso_amor",
            "namorados_urso_ferrero", "natal_panetone", "natal_papai_noel", "natal_urso_noel",
            "pais_balao_azul", "pais_balao_preto", "pais_corona", "pais_heineken",
            "pais_urso_vinho", "vinhos_bau", "vinhos_cesta_palha", "vinhos_champanhe"
        };

        foreach (Produto produto in context.Produtos.ToList())                       // passa por todas as cestas
        {
            string nome = System.IO.Path.GetFileNameWithoutExtension(produto.imagem ?? "");  // ex.: "maes_balao_rosa"
            if (comAngulos.Contains(nome))                                             // essa cesta tem pasta de ângulos?
            {
                for (int n = 1; n <= 3; n++)                                           // 1.jpg, 2.jpg e 3.jpg
                {
                    // uma linha na tabela para cada foto: o produto dono e o caminho
                    context.ProdutoImagens.Add(new ProdutoImagem { produto = produto, arquivo = "/imgs/produtos/angulos/" + nome + "/" + n + ".jpg" });
                }
            }
        }

        context.SaveChanges();  // grava tudo de uma vez
    }

    // Coloca no banco as 6 categorias e as cestas do site (mesma ideia do DbInitializer das aulas).
    // Só roda uma vez: se já tem categoria no banco, não faz nada.
    private static void CriarCestas(Context context)
    {
        if (context.Categorias.Any()) return;  // já tem categoria: não faz nada

        // as 6 categorias, nesta ordem: o menu usa os números 1 a 6
        var natal = new Categoria { nome = "Natal", imagem = "/imgs/categorias/natal.jpg" };
        var namorados = new Categoria { nome = "Namorados", imagem = "/imgs/categorias/namorados.jpg" };
        var pais = new Categoria { nome = "Dia dos Pais", imagem = "/imgs/categorias/pais.jpg" };
        var maes = new Categoria { nome = "Dia das Mães", imagem = "/imgs/categorias/maes.jpg" };
        var aniversario = new Categoria { nome = "Aniversário", imagem = "/imgs/categorias/aniversario.jpg" };
        var vinhos = new Categoria { nome = "Vinhos", imagem = "/imgs/categorias/vinhos.jpg" };
        // grava uma de cada vez para os números saírem na ordem certa (1 a 6)
        context.Categorias.Add(natal); context.SaveChanges();
        context.Categorias.Add(namorados); context.SaveChanges();
        context.Categorias.Add(pais); context.SaveChanges();
        context.Categorias.Add(maes); context.SaveChanges();
        context.Categorias.Add(aniversario); context.SaveChanges();
        context.Categorias.Add(vinhos); context.SaveChanges();

        string cartao = " Acompanha cartão com a sua mensagem.";  // final igual em todas as descrições

        // cada linha é uma cesta: nome, foto, preço, estoque, descrição e categoria
        context.Produtos.AddRange(
            new Produto { nome = "Cesta Panetone", imagem = "/imgs/produtos/natal_panetone.jpg", preco = 95, quantidade = 10, descricao = "Cesta de Natal." + cartao, categoria = natal },
            new Produto { nome = "Kit Papai Noel", imagem = "/imgs/produtos/natal_papai_noel.jpg", preco = 110, quantidade = 10, descricao = "Cesta de Natal." + cartao, categoria = natal },
            new Produto { nome = "Cesta Urso Noel", imagem = "/imgs/produtos/natal_urso_noel.jpg", preco = 130, quantidade = 10, descricao = "Cesta de Natal." + cartao, categoria = natal },
            new Produto { nome = "Cesta coração", imagem = "/imgs/produtos/namorados_coracao.jpg", preco = 120, quantidade = 10, descricao = "Presente de Namorados." + cartao, categoria = namorados },
            new Produto { nome = "Balão Eu te amo", imagem = "/imgs/produtos/namorados_eu_te_amo.jpg", preco = 135, quantidade = 10, descricao = "Presente de Namorados." + cartao, categoria = namorados },
            new Produto { nome = "Cesta urso e Ferrero", imagem = "/imgs/produtos/namorados_urso_ferrero.jpg", preco = 130, quantidade = 10, descricao = "Presente de Namorados." + cartao, categoria = namorados },
            new Produto { nome = "Bandeja Eu te amo", imagem = "/imgs/produtos/namorados_bandeja_eu_te_amo.jpg", preco = 125, quantidade = 10, descricao = "Presente de Namorados." + cartao, categoria = namorados },
            new Produto { nome = "Cesta Love", imagem = "/imgs/produtos/namorados_love.jpg", preco = 160, quantidade = 10, descricao = "Presente de Namorados." + cartao, categoria = namorados },
            new Produto { nome = "Bandeja amor com urso", imagem = "/imgs/produtos/namorados_urso_amor.jpg", preco = 145, quantidade = 10, descricao = "Presente de Namorados." + cartao, categoria = namorados },
            new Produto { nome = "Balão Dia dos Pais azul", imagem = "/imgs/produtos/pais_balao_azul.jpg", preco = 140, quantidade = 10, descricao = "Presente de Dia dos Pais." + cartao, categoria = pais },
            new Produto { nome = "Balão Dia dos Pais preto", imagem = "/imgs/produtos/pais_balao_preto.jpg", preco = 140, quantidade = 10, descricao = "Presente de Dia dos Pais." + cartao, categoria = pais },
            new Produto { nome = "Cesta vinho e chocolate", imagem = "/imgs/produtos/pais_urso_vinho.jpg", preco = 150, quantidade = 10, descricao = "Presente de Dia dos Pais." + cartao, categoria = pais },
            new Produto { nome = "Caixa Heineken e petiscos", imagem = "/imgs/produtos/pais_heineken.jpg", preco = 120, quantidade = 10, descricao = "Presente de Dia dos Pais." + cartao, categoria = pais },
            new Produto { nome = "Caixa de madeira Corona", imagem = "/imgs/produtos/pais_corona.jpg", preco = 130, quantidade = 10, descricao = "Presente de Dia dos Pais." + cartao, categoria = pais },
            new Produto { nome = "Cesta flores e caneca", imagem = "/imgs/produtos/maes_flores_caneca.jpg", preco = 120, quantidade = 10, descricao = "Presente de Dia das Mães." + cartao, categoria = maes },
            new Produto { nome = "Balão Dia das Mães", imagem = "/imgs/produtos/maes_balao_rosa.jpg", preco = 140, quantidade = 10, descricao = "Presente de Dia das Mães." + cartao, categoria = maes },
            new Produto { nome = "Balão e cesta para mães", imagem = "/imgs/produtos/maes_balao_roxo.jpg", preco = 150, quantidade = 10, descricao = "Presente de Dia das Mães." + cartao, categoria = maes },
            new Produto { nome = "Tábua de frios com balão", imagem = "/imgs/produtos/maes_balao_frios.jpg", preco = 180, quantidade = 10, descricao = "Presente de Dia das Mães." + cartao, categoria = maes },
            new Produto { nome = "Cesta orquídea rosa", imagem = "/imgs/produtos/maes_orquidea.jpg", preco = 150, quantidade = 10, descricao = "Presente de Dia das Mães." + cartao, categoria = maes },
            new Produto { nome = "Balão Parabéns", imagem = "/imgs/produtos/aniversario_parabens.jpg", preco = 150, quantidade = 10, descricao = "Presente de Aniversário." + cartao, categoria = aniversario },
            new Produto { nome = "Cesta Gratidão", imagem = "/imgs/produtos/balao_gratidao.png", preco = 150, quantidade = 10, descricao = "Presente de Aniversário." + cartao, categoria = aniversario },
            new Produto { nome = "Cesta de chocolates", imagem = "/imgs/produtos/cafe_cesta_vermelha.jpg", preco = 110, quantidade = 10, descricao = "Presente de Aniversário." + cartao, categoria = aniversario },
            new Produto { nome = "Cesta Golfinho", imagem = "/imgs/produtos/aniversario_golfinho.jpg", preco = 135, quantidade = 10, descricao = "Presente de Aniversário." + cartao, categoria = aniversario },
            new Produto { nome = "Cesta cerquinha com urso rosa", imagem = "/imgs/produtos/aniversario_cerquinha_urso.jpg", preco = 130, quantidade = 10, descricao = "Presente de Aniversário." + cartao, categoria = aniversario },
            new Produto { nome = "Baú de vinho e petiscos", imagem = "/imgs/produtos/vinhos_bau.jpg", preco = 220, quantidade = 10, descricao = "Presente com vinho." + cartao, categoria = vinhos },
            new Produto { nome = "Balde com espumante", imagem = "/imgs/produtos/vinhos_champanhe.jpg", preco = 190, quantidade = 10, descricao = "Presente com vinho." + cartao, categoria = vinhos },
            new Produto { nome = "Cesta com vinho tinto", imagem = "/imgs/produtos/vinhos_cesta_palha.jpg", preco = 170, quantidade = 10, descricao = "Presente com vinho." + cartao, categoria = vinhos }
        );
        context.SaveChanges();  // grava as cestas no banco
    }

    private static string? ColumnNullable(DbConnection connection, string table, string column)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column";
        var tableParameter = command.CreateParameter(); tableParameter.ParameterName = "@table"; tableParameter.Value = table; command.Parameters.Add(tableParameter);
        var columnParameter = command.CreateParameter(); columnParameter.ParameterName = "@column"; columnParameter.Value = column; command.Parameters.Add(columnParameter);
        return command.ExecuteScalar() as string;
    }

    private static bool TableExists(DbConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table";
        var parameter = command.CreateParameter(); parameter.ParameterName = "@table"; parameter.Value = table; command.Parameters.Add(parameter);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void Execute(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
