using Moq;
using Subscription_Service.Models;
using Subscription_Service.Services;
using Subscription_Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriptionServiceTests
{
    public class MemberServiceTests
    {
        private readonly Mock<IMemberRepository> _repo = new();
        private readonly Mock<IPaymentService> _payment = new();
        private readonly Mock<INotificationService> _notify = new();

        /// <summary>
        /// Перевіряє, що метод GetMember повертає null, якщо користувача з вказаним ID не існує в базі даних.
        /// </summary>
        [Fact]
        public void GetMember_ReturnsNull_WhenMemberDoesNotExist()
        {
            _repo.Setup(r => r.GetById(It.IsAny<int>())).Returns((Member)null);
            var service = new MemberService(_repo.Object);
            var result = service.GetMember(1);
            Assert.Null(result);
        }

        /// <summary>
        /// Перевіряє за допомогою Assert.True, що метод IsActive повертає true, коли користувач існує і є активним.
        /// </summary>
        [Fact]
        public void IsActive_MemberIsActive_ReturnsTrue()
        {
            var activeMember = new Member { Id = 1, IsActive = true };
            _repo.Setup(r => r.GetById(1)).Returns(activeMember);
            var service = new MemberService(_repo.Object);

            var result = service.IsActive(1);

            Assert.True(result);
        }

        /// <summary>
        /// Перевіряє точність отриманих даних користувача за допомогою Assert.NotNull та Assert.Equal.
        /// </summary>
        [Fact]
        public void GetMember_WhenMemberExists_ReturnsCorrectId()
        {
            int expectedId = 42;
            _repo.Setup(r => r.GetById(expectedId)).Returns(new Member { Id = expectedId });

            var service = new MemberService(_repo.Object);

            var result = service.GetMember(expectedId);

            Assert.NotNull(result);
            Assert.Equal(expectedId, result.Id);
        }
    }
}
