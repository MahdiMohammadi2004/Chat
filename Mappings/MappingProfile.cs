using AutoMapper;
using ChatApp.Models.Entities;
using ChatApp.Models.DTOs;

namespace ChatApp.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity to DTO
        CreateMap<User, UserDto>();
        
        CreateMap<ChatRoom, ChatRoomDto>()
            .ForMember(dest => dest.CreatedByUsername, opt => opt.MapFrom(src => src.CreatedByUser.Username))
            .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.Participants.Select(p => p)));
        
        CreateMap<ChatRoomParticipant, ChatRoomParticipantDto>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));
        
        CreateMap<Message, MessageDto>()
            .ForMember(dest => dest.SenderUsername, opt => opt.MapFrom(src => src.SenderUser.Username))
            .ForMember(dest => dest.SenderDisplayName, opt => opt.MapFrom(src => src.SenderUser.DisplayName));
        
        // DTO to Entity (for creation)
        CreateMap<CreateGroupChatRequest, ChatRoom>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(_ => ChatRoomType.Group))
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Participants, opt => opt.Ignore())
            .ForMember(dest => dest.Messages, opt => opt.Ignore());
    }
}
