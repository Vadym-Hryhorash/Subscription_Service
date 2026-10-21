using Moq;
using Subscription_Service.Models;
using Subscription_Service.Services;
using Subscription_Service.Services.Interfaces;

namespace SubscriptionServiceTests
{
    public class SubscriptionServiceTests
    {
        private readonly Mock<IMemberRepository> _repo = new();
        private readonly Mock<IPaymentService> _payment = new();
        private readonly Mock<INotificationService> _notify = new();


        /// <summary>
        /// Параметризований тест, що перевіряє успішне оновлення підписки з різними наборами вхідних даних.
        /// </summary>
        [Theory]
        [InlineData(1, 100.50, 30)]
        [InlineData(2, 50.00, 15)]
        [InlineData(3, 10.99, 7)]
        public void RenewSubscription_ValidData_ReturnsTrueAndUpdatesMember(int memberId, decimal paymentAmount, int days)
        {
            var member = new Member
            {
                Id = memberId,
                IsActive = false,
                SubscriptionEnd = DateTime.Now.AddDays(-1)
            };

            _repo.Setup(r => r.GetById(memberId)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(memberId, paymentAmount)).Returns(true);

            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);
            var result = service.RenewSubscription(memberId, paymentAmount, days);

            Assert.True(result);
            Assert.True(member.IsActive);
            Assert.True(member.SubscriptionEnd > DateTime.Now);
        }

        /// <summary>
        /// Перевіряє генерацію винятку ArgumentException за допомогою Assert.Throws, якщо користувача не знайдено в репозиторії.
        /// </summary>
        [Fact]
        public void RenewSubscription_MemberNotFound_ThrowsArgumentException()
        {
            _repo.Setup(r => r.GetById(It.IsAny<int>())).Returns((Member)null);
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);
            var exception = Assert.Throws<ArgumentException>(() => service.RenewSubscription(99, 100m, 30));
        }

        /// <summary>
        /// Перевіряє за допомогою Times.AtLeastOnce та Times.Never, що метод Update викликається лише для протермінованих користувачів.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_HasExpiredMembers_CallsUpdateAndNotify()
        {
            var expiredMember = new Member
            {
                Id = 1,
                IsActive = true,
                SubscriptionEnd = DateTime.Now.AddDays(-5)
            };
            var activeMember = new Member
            {
                Id = 2,
                IsActive = true,
                SubscriptionEnd = DateTime.Now.AddDays(10)
            };

            _repo.Setup(r => r.GetAll()).Returns(new List<Member> { expiredMember, activeMember });
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            service.DeactivateExpiredMembers();

            _repo.Verify(r => r.Update(It.Is<Member>(m => m.Id == 1 && m.IsActive == false)), Times.AtLeastOnce);
            _repo.Verify(r => r.Update(It.Is<Member>(m => m.Id == 2)), Times.Never);
        }

        /// <summary>
        /// Перевіряє за допомогою Assert.False, що метод RenewSubscription повертає false, якщо оплата не прошла.
        /// </summary>
        [Fact]
        public void RenewSubscription_PaymentFails_ReturnsFalse()
        {
            var member = new Member { Id = 1, IsActive = false };
            _repo.Setup(r => r.GetById(1)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(1, It.IsAny<decimal>())).Returns(false);
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            var result = service.RenewSubscription(1, 100m, 30);

            Assert.False(result);
        }

        /// <summary>
        /// Перевіряє за допомогою Verify та Times.Never, що у разі невдалої оплати сповіщення користувачу не відправляється.
        /// </summary>
        [Fact]
        public void RenewSubscription_PaymentFails_DoesNotSendNotification()
        {
            var member = new Member { Id = 1, IsActive = false };
            _repo.Setup(r => r.GetById(1)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(1, It.IsAny<decimal>())).Returns(false);
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            service.RenewSubscription(1, 100m, 30);

            _notify.Verify(n => n.SendNotification(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Перевіряє за допомогою It.IsAny<Member>(), що метод _repo.Update() викликається з будь-яким екземпляром типу Member.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_HasExpiredMembers_CallsUpdateWithAnyMember()
        {
            var expiredMember = new Member
            {
                Id = 1,
                IsActive = true,
                SubscriptionEnd = DateTime.Now.AddDays(-1)
            };
            _repo.Setup(r => r.GetAll()).Returns(new List<Member> { expiredMember });
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            service.DeactivateExpiredMembers();
            
            _repo.Verify(r => r.Update(It.IsAny<Member>()), Times.Once);
        }

        /// <summary>
        /// Перевіряє аргумент під час виклику оновлення через предикат It.Is: переконується, що об'єкту встановлено IsActive = true.
        /// </summary>
        [Fact]
        public void RenewSubscription_SuccessfulPayment_UpdatesMemberWithActiveStatus()
        {
            var member = new Member { Id = 1, IsActive = false };
            _repo.Setup(r => r.GetById(1)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(1, 100m)).Returns(true);
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            service.RenewSubscription(1, 100m, 30);
            
            _repo.Verify(r => r.Update(It.Is<Member>(m => m.IsActive == true)), Times.Once);
        }

        /// <summary>
        /// Перевіряє вміст повідомлення винятку за допомогою Assert.Contains після перехоплення винятку.
        /// </summary>
        [Fact]
        public void RenewSubscription_MemberNotFound_ExceptionMessageContainsExpectedText()
        {
            _repo.Setup(r => r.GetById(It.IsAny<int>())).Returns((Member)null);
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            var exception = Assert.Throws<ArgumentException>(() => service.RenewSubscription(999, 100m, 30));

            Assert.Contains("Member not found", exception.Message);
        }
    }

}
