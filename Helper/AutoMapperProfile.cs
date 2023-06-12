using AutoMapper;
using WebAPI.Dominio;
using WebAPI.Identity.Dto;

namespace WebAPI.Identity.Helper
{
    public class AutoMapperProfile : Profile //AutoMapper procura a classe q herda de profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>().ReverseMap(); //ReverseMap serve para fazer ao inverso TAMBÉM.
            CreateMap<User, UserLoginDto>().ReverseMap();
        }
    }
}
