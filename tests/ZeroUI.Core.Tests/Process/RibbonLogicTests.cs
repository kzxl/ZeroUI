using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ZeroUI.Core.Tests.Process
{
    public class RibbonLogicTests
    {
        public class TestApprovalItem
        {
            public string Key { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public class TestApprovalGroup
        {
            public string GroupKey { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public bool Visible { get; set; } = true;
            public List<TestApprovalItem> Items { get; } = new List<TestApprovalItem>();

            public int TotalCount => Items.Sum(i => Math.Max(0, i.Count));

            public void UpdateCount(string key, int count)
            {
                var item = Items.FirstOrDefault(i => i.Key == key);
                if (item != null) item.Count = count;
            }
        }

        [Fact]
        public void ApprovalGroup_TotalCount_CalculatesSumOfNonNegativeCounts()
        {
            var grp = new TestApprovalGroup { GroupKey = "Common1", Title = "Chờ duyệt chung 1" };
            grp.Items.Add(new TestApprovalItem { Key = "YCSL", Name = "Yêu cầu xử lý", Count = 3 });
            grp.Items.Add(new TestApprovalItem { Key = "YCTN", Name = "Yêu cầu thử nghiệm", Count = 1 });
            grp.Items.Add(new TestApprovalItem { Key = "BG", Name = "Bàn giao", Count = 0 });

            Assert.Equal(4, grp.TotalCount);
        }

        [Fact]
        public void ApprovalGroup_UpdateCount_UpdatesTargetItemAccurately()
        {
            var grp = new TestApprovalGroup { GroupKey = "Production", Title = "Chờ duyệt SX" };
            grp.Items.Add(new TestApprovalItem { Key = "LSX", Name = "Lệnh sản xuất", Count = 5 });
            grp.Items.Add(new TestApprovalItem { Key = "NKDG", Name = "Nhật ký đóng gói", Count = 2 });

            Assert.Equal(7, grp.TotalCount);

            grp.UpdateCount("LSX", 0);
            Assert.Equal(2, grp.TotalCount);

            grp.UpdateCount("NKDG", 8);
            Assert.Equal(8, grp.TotalCount);
        }

        [Fact]
        public void ApprovalGroup_VisibilityToggle_ControlsDisplayStatus()
        {
            var groups = new List<TestApprovalGroup>
            {
                new TestApprovalGroup { GroupKey = "Common1", Title = "Chờ duyệt chung 1", Visible = true },
                new TestApprovalGroup { GroupKey = "Common2", Title = "Chờ duyệt chung 2", Visible = true },
                new TestApprovalGroup { GroupKey = "Production", Title = "Chờ duyệt SX", Visible = false }
            };

            var visibleTitles = groups.Where(g => g.Visible).Select(g => g.Title).ToList();
            Assert.Equal(2, visibleTitles.Count);
            Assert.Contains("Chờ duyệt chung 1", visibleTitles);
            Assert.Contains("Chờ duyệt chung 2", visibleTitles);
            Assert.DoesNotContain("Chờ duyệt SX", visibleTitles);

            // Toggle Production visible
            groups.First(g => g.GroupKey == "Production").Visible = true;
            Assert.Equal(3, groups.Count(g => g.Visible));
        }

        [Theory]
        [InlineData("Thông báo", "1178", "Thông báo (1178)")]
        [InlineData("Góc nhìn 360", "1", "Góc nhìn 360 (1)")]
        [InlineData("Công việc", null, "Công việc")]
        public void RibbonItem_LabelWithBadge_FormatsNicely(string title, string? badge, string expected)
        {
            string formatted = string.IsNullOrEmpty(badge) ? title : $"{title} ({badge})";
            Assert.Equal(expected, formatted);
        }
    }
}
