using Business;
using Microsoft.EntityFrameworkCore;

namespace Repository.EF;

public class Context : DbContext
{
    public Context(DbContextOptions<Context> options) : base(options) { }
    
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<ProdutoImagem> ProdutoImagens { get; set; }  // tabela das fotos extras dos produtos
        public DbSet<Categoria> Categorias { get; set; }  // tabela de categorias
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Endereco> Enderecos { get; set; }
        public DbSet<Produto_Pedido>  ProdutoPedidos { get; set; }
        public DbSet<Telefone_Cliente>  TelefoneClientes { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<CidadeEntrega> CidadesEntrega { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cliente>(t =>
                {
                    t.ToTable("Clientes");
                    t.HasKey(t=>t.id);
                    t.Property(t=>t.id).
                        HasColumnType("int").
                        IsRequired().ValueGeneratedOnAdd();

                    t.Property(t => t.nome).
                        HasColumnType("text").
                        IsRequired();
                    t.Property(t=>t.email).
                        HasColumnType("varchar(100)").
                        IsRequired();
                    t.Property(t=>t.login).
                        HasColumnType("varchar(100)").
                        IsRequired();
                    t.Property(t=>t.senha).
                        HasColumnType("varchar(100)").
                        IsRequired();
                    
                    t.HasMany(t=>t.Enderecos).
                        WithOne(t=>t.Cliente).
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired();
                    
                    t.HasMany(t=>t.pedidos).
                        WithOne(t=>t.cliente).
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired();
                    
                    t.HasMany(t=>t.telefones).
                        WithOne(t=>t.cliente).
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired();
                }
            );
            modelBuilder.Entity<Endereco>(t =>
                {
                    t.ToTable("Enderecos");
                    t.HasKey(t=>t.id);
                    t.Property(t=>t.id).
                        HasColumnType("int").
                        IsRequired().ValueGeneratedOnAdd();
                    
                    t.Property(t=>t.logradouro).
                        HasColumnType("text").
                        IsRequired();
                    t.Property(t=>t.numero).HasColumnType("varchar(10)").IsRequired(false);  // número da casa
                    t.Property(t=>t.bairro).
                        HasColumnType("varchar(100)").
                        IsRequired();
                    t.Property(t=>t.cep).
                        HasColumnType("varchar(9)").
                        IsRequired();
                    t.Property(t=>t.cidade).
                        HasColumnType("varchar(100)").
                        IsRequired();
                    t.Property(t=>t.complemento).
                        HasColumnType("varchar(100)").
                        IsRequired(false);
                    
                    t.HasOne(t=>t.Cliente).
                        WithMany(t=>t.Enderecos).
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired();
                }
            );

            modelBuilder.Entity<Pedido>(t =>
                {
                    t.ToTable("pedidos");
                    t.HasKey(t=>t.id);
                    t.Property(t=>t.id).
                        HasColumnType("int").
                        IsRequired().ValueGeneratedOnAdd();
                    
                    t.Property(t=>t.data_pedido).
                        HasColumnType("datetime").
                        IsRequired();
                    t.Property(t => t.observacao).
                        HasColumnType("text");
                    t.Property(t=>t.cartao_texto).
                        HasColumnType("text").
                        IsRequired();
                    t.Property(t=>t.status_pedido).
                        HasColumnType("varchar(20)").
                        IsRequired();
                    t.Property(t=>t.tipo_pagamento).
                        HasColumnType("varchar(20)").
                        IsRequired();
                    t.Property(t=>t.nome_recebe).HasColumnType("varchar(60)").IsRequired(false);  // nome de quem recebe
                    t.Property(t=>t.taxa_entrega).HasColumnType("decimal(12,2)");
                    
                    t.HasOne(t=>t.cliente).
                        WithMany(t=>t.pedidos).
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired();
                    
                    t.HasOne(t=>t.endereco).
                        WithOne(t=>t.pedido).
                        HasForeignKey<Pedido>("enderecoId").
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired(false);
                    
                    t.HasOne(t=>t.usuario).
                        WithMany(t=>t.pedidos).
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired(false);
                }
            );
            // mapeamento da tabela de categorias (mesmo molde das aulas)
            modelBuilder.Entity<Categoria>(t =>
                {
                    t.ToTable("Categorias");                                   // nome da tabela no banco
                    t.HasKey(t=>t.id);                                         // id é a chave primária
                    t.Property(t=>t.id).ValueGeneratedOnAdd();                 // o banco gera o número sozinho
                    t.Property(t=>t.nome).HasColumnType("varchar(100)").IsRequired();     // nome obrigatório
                    t.Property(t=>t.imagem).HasColumnType("varchar(255)").IsRequired(false);  // foto opcional
                }
            );
            // mapeamento das fotos extras: uma linha por foto (a principal continua em produtos.imagem)
            modelBuilder.Entity<ProdutoImagem>(t =>
                {
                    t.ToTable("ProdutoImagens");                                          // nome da tabela no banco
                    t.HasKey(t=>t.id);                                                    // id é a chave primária
                    t.Property(t=>t.id).ValueGeneratedOnAdd();                            // o banco gera o número sozinho
                    t.Property(t=>t.arquivo).HasColumnType("varchar(255)").IsRequired();  // caminho da foto, obrigatório
                    t.HasOne(t=>t.produto).                                               // cada foto é de um produto
                        WithMany(t=>t.imagens).                                           // e um produto tem várias fotos
                        HasForeignKey(t=>t.produtoId).                                    // coluna com o número do produto
                        OnDelete(DeleteBehavior.NoAction).                                // o ProdutoServices apaga as fotos antes do produto
                        IsRequired();                                                     // toda foto tem dono
                }
            );
            modelBuilder.Entity<Produto>(t =>
                {
                    t.ToTable("produtos");
                    t.HasKey(t=>t.id);
                    t.Property(t=>t.id).
                        HasColumnType("int").
                        IsRequired().ValueGeneratedOnAdd();
                    
                    t.Property(t=>t.nome).
                        HasColumnType("text").
                        IsRequired();
                    t.Property(t=>t.descricao).
                        HasColumnType("text").
                        IsRequired();
                    t.Property(t=>t.preco).
                        HasColumnType("double").
                        IsRequired();
                    t.Property(t => t.imagem).HasColumnType("varchar(255)").IsRequired(false);
                    t.Property(t=>t.quantidade).
                        HasColumnType("int").
                        IsRequired();
                    
                    // cada produto pertence a uma categoria, e cada categoria tem muitos produtos
                    t.HasOne(t=>t.categoria).
                        WithMany(t=>t.produtos).
                        HasForeignKey(t=>t.categoriaId).        // coluna que guarda o número da categoria
                        OnDelete(DeleteBehavior.NoAction).      // apagar a categoria não apaga os produtos
                        IsRequired(false);                      // os produtos de teste antigos não têm categoria

                    t.HasMany(t=>t.produtoPedidos).
                        WithOne(t=>t.produto).
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired();
                    
                }
            );
            modelBuilder.Entity<Produto_Pedido>(t =>
                {
                    t.Property<int>("pedidoId");
                    t.Property<int>("produtoId");
                    t.HasKey(t => t.id);
                    t.Property(t=>t.preco_unitario).HasColumnType("decimal(12,2)");
                   
                    t.Property(t=>t.quantidade).
                        HasColumnType("int").
                        IsRequired();
                    t.Property(t => t.DescricaoCoresDecoracao).
                        HasColumnType("text").
                        IsRequired();
                    
                    t.HasOne(t=>t.pedido).
                        WithMany(t=>t.produtos).
                        HasForeignKey("pedidoId").
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired();
                    
                    t.HasOne(t=>t.produto).
                        WithMany(t=>t.produtoPedidos).
                        HasForeignKey("produtoId").
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired();
                }
            );
            modelBuilder.Entity<Telefone_Cliente>(t =>
                {
                    t.ToTable("TelefoneClientes");
                    t.HasKey(t=>t.id);
                    t.Property(t=>t.id).
                        HasColumnType("int").
                        IsRequired().ValueGeneratedOnAdd();
                    
                    t.Property(t=>t.numero).
                        HasColumnType("varchar(20)").
                        IsRequired();
                    
                    t.HasOne(t=>t.cliente).
                        WithMany(t=>t.telefones).
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired();
                }
            );
            modelBuilder.Entity<CidadeEntrega>(t =>
            {
                t.ToTable("CidadesEntrega");
                t.HasKey(c => c.id);
                t.Property(c => c.nome).HasColumnType("varchar(100)").IsRequired();
                t.HasIndex(c => c.nome).IsUnique();
                t.Property(c => c.taxa).HasColumnType("decimal(12,2)");
            });
            modelBuilder.Entity<Usuario>(t =>
                {
                    t.ToTable("Usuarios");
                    t.HasIndex(u => u.login).IsUnique();
                    t.HasIndex(u => u.email).IsUnique();
                    t.HasKey(t=>t.id);
                    t.Property(t=>t.id).
                        HasColumnType("int").
                        IsRequired().ValueGeneratedOnAdd();
                    
                    t.Property(t=>t.nome).
                        HasColumnType("text").
                        IsRequired();
                    t.Property(t=>t.email).
                        HasColumnType("varchar(100)").
                        IsRequired();
                    t.Property(t=>t.login).
                        HasColumnType("varchar(100)").
                        IsRequired();
                    t.Property(t=>t.senha).
                        HasColumnType("varchar(256)").
                        IsRequired();
                    t.Property(t=>t.nivel).
                        HasColumnType("varchar(100)").
                        IsRequired();
                    
                    t.HasMany(t=>t.pedidos).
                        WithOne(t=>t.usuario).
                        OnDelete(DeleteBehavior.NoAction).
                        IsRequired(false);
                }
            );
        }
}
