using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using WebAPI.Dominio;
using WebAPI.Identity.Helper;
using WebAPI.Repository;

namespace WebAPI.Identity
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection"); //Pegando a string de conexão do AppSettings.
            var migrationAssembly = typeof(Startup) //Variável criada para evitar o erro de assembly no cmd.
              .GetTypeInfo().Assembly
              .GetName().Name;


            services.AddDbContext<Context>(
                opt => opt.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(migrationAssembly)) //Lambda usada para que seja realizado uma migrationAssembly para cada consulta no sql.
            );

            services.AddIdentityCore<User>(options => //Configurações de required
            {
                //options.SignIn.RequireConfirmedEmail = true; //-> Confirmação de email

                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 4;

                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.AllowedForNewUsers = true;

            }) //Serviço para utilizar o User com IdentityCore.
            .AddRoles<Role>()
            .AddEntityFrameworkStores<Context>() //Utilizando o AddIdentity sem ser com o Core, para poder passar como tipo, o IdentityRole, que é um identitficador de permissão
            .AddRoleValidator<RoleValidator<Role>>()    //Caso seja criado um novo campo, basta rodar novamente a migration.
            .AddRoleManager<RoleManager<Role>>()        //3 Linhas adicionadas para que continuem trabalhando com o user, para tudo que seja adicionado.
            .AddSignInManager<SignInManager<User>>()
            .AddDefaultTokenProviders(); //Provedor de token padrão

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) //Header.Payload.VerifySignature
                .AddJwtBearer(options => //Se o JWT se encaixar aqui, ele valida e passa para as minhas controllers.
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true, //Emissor de chave
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                            .GetBytes(Configuration.GetSection("AppSettings:Token").Value)), //Está presente no appsettings
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.AddMvc(options => //Toda vez que uma controller for instanciada, eu não preciso ir em cima da controller em si e colocar [Authorize]
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(opt => opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore); //Ele ignora loops

            //services.AddAutoMapper(); //Configurando o AutoMapper -> N funciona mais...

            var mappingConfig = new MapperConfiguration(mc =>  // variável recebendo a configuração do mapper
            {
                mc.AddProfile(new AutoMapperProfile()); //Configuração por lambda, adicionando um perfil de mapeamento de domínio e dto.
            });

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.AddCors(); //Requisição cruzada
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication(); //Sem isso, minhas autenticações não funcionam...

            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); //Para todas as origens, métodos e cabeçalhos.
            app.UseMvc();
        }
    }
}
