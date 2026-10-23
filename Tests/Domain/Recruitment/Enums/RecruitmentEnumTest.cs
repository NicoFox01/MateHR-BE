using MateHR.Domain.Recruitment.Enums;


namespace MateHR.Tests.Domain.Recruitment.Enums
{
    public class RecruitmentEnumTest
    {
        [Fact]
        public void SeniorityLevel_Trainee_IsOne()
        {
            //Assert
            Assert.Equal(1, (int)SeniorityLevel.Trainee);
        }
        [Fact]
        public void ClientType_Internal_IsOne()
        {
            //Assert
            Assert.Equal(1, (int)ClientType.Internal);
        }
        [Fact]
        public void Provincia_BuenosAires_IsOne()
        {
            //Assert
            Assert.Equal(1, (int)Province.BuenosAires);
        }
        [Fact]
        public void Country_Argentina_IsOne()
        {
            //Assert
            Assert.Equal(1, (int)Country.Argentina);
        }

    }
}
