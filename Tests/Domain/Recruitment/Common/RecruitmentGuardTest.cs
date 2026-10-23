using MateHR.Domain.Recruitment.Common;
using MateHR.Domain.Recruitment.Enums;


namespace MateHR.Tests.Domain.Recruitment.Common
{
    public class RecruitmentGuardTest
    {
        [Fact]
        public void RequiredName_Whitespace_ThrowsArgumentException()
        {
            //Assert
            Assert.Throws<ArgumentException>(() => RecruitmentGuard.RequiredName("   ", 100));
        }
        [Fact]
        public void RequiredName_TooLong_ThrowsArgumentException()
        {
            //Assert
            Assert.Throws<ArgumentException>(() => RecruitmentGuard.RequiredName("This is a very long name that exceeds the maximum allowed length", 10));
        }
        [Fact]
        public void RequiredName_TrimsValue()
        {
            //Assert
            Assert.Equal("John Doe", RecruitmentGuard.RequiredName("   John Doe   ", 100));
        }
        [Fact]
        public void ValidEnum_Undefined_ThrowsArgumentOutOfRangeException()
        {
            //Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => RecruitmentGuard.ValidEnum((SeniorityLevel)100));
        }
        [Fact]
        public void NotEmptyGuid_Empty_ThrowsArgumentException()
        {
            //Assert
            Assert.Throws<ArgumentException>(() => RecruitmentGuard.NotEmptyGuid(Guid.Empty));
        }
        [Fact]
        public void NonNegative_Negative_ThrowsArgumentOutOfRangeException()
        {
            //Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => RecruitmentGuard.NonNegative(-1));
        }

    }
}
