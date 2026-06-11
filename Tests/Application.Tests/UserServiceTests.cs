using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Users;
using Application.Interfaces;
using Application.Services; 
using Infrastructure.Services;
using Domain.Entities;
using Moq;
using Xunit;

namespace Application.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ICloudinaryService> _cloudinaryMock;
        private readonly Mock<INotificationService> _notificationMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _userRepoMock = new Mock<IUserRepository>();
            _cloudinaryMock = new Mock<ICloudinaryService>();
            _notificationMock = new Mock<INotificationService>();
            _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
            _userService = new UserService(_uowMock.Object, _cloudinaryMock.Object, _notificationMock.Object);
        }


        [Fact]
        public async Task SyncUserAsync_NewUser_ShouldCreateUserInRepository()
        {
            var syncDto = new SyncUserDto 
            { 
                FirebaseUid = "test_uid_123", 
                Email = "estudiante@uta.edu.ec", 
                DisplayName = "Manuel Ramirez" 
            };
            
            _userRepoMock.Setup(repo => repo.GetByUidAsync(syncDto.FirebaseUid))
                .ReturnsAsync((User?)null);

            var result = await _userService.SyncUserAsync(syncDto);

            _userRepoMock.Verify(repo => repo.AddAsync(It.Is<User>(u => u.Email == syncDto.Email)), Times.Once);
            _uowMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            Assert.NotNull(result);
            Assert.Equal("estudiante@uta.edu.ec", result.Email);
        }

        [Fact]
        public async Task UpdateProfileAsync_ValidData_ShouldUpdateUserFields()
        {
            var firebaseUid = "test_uid_123";
            var existingUser = new User { FirebaseUid = firebaseUid, DisplayName = "Old Name" };
            var updateDto = new UpdateProfileDto 
            { 
                DisplayName = "Cristian Jurado", 
                Zone = "Ficoa", 
                Career = "Software",
                Phone = "0999999999"
            };

            _userRepoMock.Setup(repo => repo.GetByUidAsync(firebaseUid))
                .ReturnsAsync(existingUser);

            var result = await _userService.UpdateProfileAsync(firebaseUid, updateDto);

            _userRepoMock.Verify(repo => repo.Update(It.Is<User>(u => u.Zone == "Ficoa" && u.DisplayName == "Cristian Jurado")), Times.Once);
            _uowMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            Assert.Equal("Cristian Jurado", result.DisplayName);
            Assert.Equal("Ficoa", result.Zone);
        }

        [Fact]
        public async Task UpdateProfileAsync_UserNotFound_ShouldThrowException()
        {
            var firebaseUid = "not_found_uid";
            var updateDto = new UpdateProfileDto { DisplayName = "Nuevo Nombre" };

            _userRepoMock.Setup(repo => repo.GetByUidAsync(firebaseUid))
                .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.UpdateProfileAsync(firebaseUid, updateDto));
        }
    }
}
