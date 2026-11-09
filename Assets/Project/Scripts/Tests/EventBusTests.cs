using System;
using System.Collections.Generic;
using System.Reflection;
using Kitchen.Core.Data;
using Kitchen.Core.Events;
using NUnit.Framework;

namespace Kitchen.Core.Tests
{
	// 注意：EventBus 會 catch 訂閱者拋出的例外（含 NUnit 的 AssertionException），
	// 因此訂閱回呼內只記錄資料，斷言一律寫在 Publish 之後。
	public class EventBusTests
	{
		[SetUp]
		public void SetUp()
		{
			EventBus.ResetAll();
		}

		[TearDown]
		public void TearDown()
		{
			EventBus.ResetAll();
		}

		// ---- helpers ----

		private static List<T> Capture<T>(EventChannel<T> channel)
		{
			var received = new List<T>();
			channel.Subscribe(received.Add);
			return received;
		}

		private static List<string> CaptureErrors()
		{
			var errors = new List<string>();
			EventBus.ErrorReporter = errors.Add;
			return errors;
		}

		private static DishRecord MakeRecord()
		{
			return new DishRecord("tomato_egg_stirfry", DishRoutes.Sour, "S", 4,
				new DishAffix("sour_ingredient_bonus", 0.124f), "Chef_Lin", 1782541920L, 0.93f);
		}

		private static IEnumerable<FieldInfo> ChannelFields()
		{
			foreach (FieldInfo field in typeof(EventBus).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
			{
				if (typeof(EventChannelBase).IsAssignableFrom(field.FieldType)) yield return field;
			}
		}

		private static void Noop<T>(T _)
		{
		}

		/// <summary>以反射替任意頻道加一個空訂閱者（泛型或無參數版皆可）。</summary>
		private static void SubscribeNoop(EventChannelBase channel)
		{
			Type type = channel.GetType();
			Delegate handler;
			if (type == typeof(EventChannel))
			{
				handler = (Action)(() => { });
			}
			else
			{
				Type payloadType = type.GetGenericArguments()[0];
				MethodInfo noop = typeof(EventBusTests)
					.GetMethod(nameof(Noop), BindingFlags.NonPublic | BindingFlags.Static)
					.MakeGenericMethod(payloadType);
				handler = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(payloadType), noop);
			}
			type.GetMethod("Subscribe").Invoke(channel, new object[] { handler });
		}

		// ---- 每個頻道發布/接收 ----

		[Test]
		public void GoldChanged_Publish_DeliversPayload()
		{
			var received = Capture(EventBus.GoldChanged);

			EventBus.GoldChanged.Publish(new GoldChangedPayload(480, -20));

			Assert.AreEqual(1, received.Count);
			Assert.AreEqual(480L, received[0].NewValue);
			Assert.AreEqual(-20L, received[0].Delta);
		}

		[Test]
		public void InventoryChanged_Publish_DeliversPayload()
		{
			var received = Capture(EventBus.InventoryChanged);

			EventBus.InventoryChanged.Publish(new InventoryChangedPayload("tomato", 4));

			Assert.AreEqual(1, received.Count);
			Assert.AreEqual("tomato", received[0].IngredientId);
			Assert.AreEqual(4, received[0].NewCount);
		}

		[Test]
		public void IngredientGenerated_Publish_DeliversPayload()
		{
			var received = Capture(EventBus.IngredientGenerated);

			EventBus.IngredientGenerated.Publish("egg");

			CollectionAssert.AreEqual(new[] { "egg" }, received);
		}

		[Test]
		public void OrderConfirmed_Publish_DeliversPayload()
		{
			var received = Capture(EventBus.OrderConfirmed);
			var recipe = new Recipe("酸香番茄炒蛋", new[] { "tomato", "tomato", "egg" });

			EventBus.OrderConfirmed.Publish(recipe);

			Assert.AreEqual(1, received.Count);
			Assert.AreSame(recipe, received[0]);
		}

		[Test]
		public void OrderStateChanged_Publish_DeliversEveryState()
		{
			var received = Capture(EventBus.OrderStateChanged);
			var states = (OrderState[])Enum.GetValues(typeof(OrderState));

			foreach (OrderState state in states)
			{
				EventBus.OrderStateChanged.Publish(state);
			}

			CollectionAssert.AreEqual(states, received);
		}

		[Test]
		public void EarlyCookRequested_Publish_InvokesSubscriber()
		{
			int calls = 0;
			EventBus.EarlyCookRequested.Subscribe(() => calls++);

			EventBus.EarlyCookRequested.Publish();

			Assert.AreEqual(1, calls);
		}

		[Test]
		public void DishCompleted_Publish_DeliversPayload()
		{
			var received = Capture(EventBus.DishCompleted);
			DishRecord record = MakeRecord();

			EventBus.DishCompleted.Publish(record);

			Assert.AreEqual(1, received.Count);
			Assert.AreSame(record, received[0]);
		}

		[Test]
		public void RecordShelved_Publish_DeliversPayload()
		{
			var received = Capture(EventBus.RecordShelved);

			EventBus.RecordShelved.Publish(new RecordShelvedPayload(3, 1));

			Assert.AreEqual(1, received.Count);
			Assert.AreEqual(3, received[0].RecordIndex);
			Assert.AreEqual(1, received[0].Slot);
		}

		[Test]
		public void ModifiersChanged_Publish_InvokesSubscriber()
		{
			int calls = 0;
			EventBus.ModifiersChanged.Subscribe(() => calls++);

			EventBus.ModifiersChanged.Publish();

			Assert.AreEqual(1, calls);
		}

		[Test]
		public void UpgradePurchased_Publish_DeliversPayload()
		{
			var received = Capture(EventBus.UpgradePurchased);

			EventBus.UpgradePurchased.Publish(new UpgradePurchasedPayload("fridge_speed", 3));

			Assert.AreEqual(1, received.Count);
			Assert.AreEqual("fridge_speed", received[0].UpgradeId);
			Assert.AreEqual(3, received[0].NewLevel);
		}

		[Test]
		public void SaveRequested_Publish_DeliversPayload()
		{
			var received = Capture(EventBus.SaveRequested);

			EventBus.SaveRequested.Publish(new SaveRequestedPayload("dish_completed"));

			Assert.AreEqual(1, received.Count);
			Assert.AreEqual("dish_completed", received[0].Reason);
		}

		[Test]
		public void Publish_MultipleSubscribers_AllReceiveInSubscriptionOrder()
		{
			var order = new List<string>();
			EventBus.IngredientGenerated.Subscribe(id => order.Add("a:" + id));
			EventBus.IngredientGenerated.Subscribe(id => order.Add("b:" + id));

			EventBus.IngredientGenerated.Publish("salt");

			CollectionAssert.AreEqual(new[] { "a:salt", "b:salt" }, order);
		}

		[Test]
		public void Publish_NoSubscribers_DoesNotThrowOrReport()
		{
			var errors = CaptureErrors();

			Assert.DoesNotThrow(() => EventBus.GoldChanged.Publish(new GoldChangedPayload(0, 0)));
			Assert.DoesNotThrow(() => EventBus.ModifiersChanged.Publish());
			Assert.IsEmpty(errors);
		}

		// ---- 訂閱 / 取消 ----

		[Test]
		public void Subscribe_Null_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => EventBus.GoldChanged.Subscribe(null));
			Assert.Throws<ArgumentNullException>(() => EventBus.ModifiersChanged.Subscribe(null));
		}

		[Test]
		public void Subscribe_SameHandlerTwice_ReceivesOnce()
		{
			var received = new List<string>();
			Action<string> handler = received.Add;

			EventBus.IngredientGenerated.Subscribe(handler);
			EventBus.IngredientGenerated.Subscribe(handler);
			EventBus.IngredientGenerated.Publish("egg");

			Assert.AreEqual(1, EventBus.IngredientGenerated.SubscriberCount);
			Assert.AreEqual(1, received.Count);
		}

		[Test]
		public void Unsubscribe_StopsDelivery()
		{
			var received = new List<string>();
			Action<string> handler = received.Add;
			EventBus.IngredientGenerated.Subscribe(handler);

			EventBus.IngredientGenerated.Unsubscribe(handler);
			EventBus.IngredientGenerated.Publish("egg");

			Assert.IsEmpty(received);
			Assert.AreEqual(0, EventBus.IngredientGenerated.SubscriberCount);
		}

		[Test]
		public void Unsubscribe_ParameterlessChannel_StopsDelivery()
		{
			int calls = 0;
			Action handler = () => calls++;
			EventBus.EarlyCookRequested.Subscribe(handler);

			EventBus.EarlyCookRequested.Unsubscribe(handler);
			EventBus.EarlyCookRequested.Publish();

			Assert.AreEqual(0, calls);
		}

		[Test]
		public void Unsubscribe_OnlyRemovesThatHandler()
		{
			var a = new List<string>();
			var b = new List<string>();
			Action<string> handlerA = a.Add;
			EventBus.IngredientGenerated.Subscribe(handlerA);
			EventBus.IngredientGenerated.Subscribe(b.Add);

			EventBus.IngredientGenerated.Unsubscribe(handlerA);
			EventBus.IngredientGenerated.Publish("egg");

			Assert.IsEmpty(a);
			Assert.AreEqual(1, b.Count);
		}

		[Test]
		public void Unsubscribe_NotSubscribedOrNull_DoesNothing()
		{
			Assert.DoesNotThrow(() => EventBus.IngredientGenerated.Unsubscribe(_ => { }));
			Assert.DoesNotThrow(() => EventBus.IngredientGenerated.Unsubscribe(null));
			Assert.DoesNotThrow(() => EventBus.EarlyCookRequested.Unsubscribe(null));
		}

		// ---- 同頻道重入 ----

		[Test]
		public void SameChannelReentry_NestedPublishNotDispatched_AndReported()
		{
			var errors = CaptureErrors();
			var received = new List<long>();
			EventBus.GoldChanged.Subscribe(p =>
			{
				received.Add(p.NewValue);
				EventBus.GoldChanged.Publish(new GoldChangedPayload(999, 1));
			});

			EventBus.GoldChanged.Publish(new GoldChangedPayload(10, 10));

			CollectionAssert.AreEqual(new[] { 10L }, received, "Nested publish must not be dispatched.");
			Assert.AreEqual(1, errors.Count);
			StringAssert.Contains("'GoldChanged'", errors[0]);
			StringAssert.Contains("re-entrant", errors[0]);
			// 呼叫堆疊應包含本測試類別的呼叫框
			StringAssert.Contains(nameof(EventBusTests), errors[0]);
			Assert.IsFalse(EventBus.GoldChanged.IsDispatching);
		}

		[Test]
		public void SameChannelReentry_ParameterlessChannel_NotDispatched_AndReported()
		{
			var errors = CaptureErrors();
			int calls = 0;
			EventBus.EarlyCookRequested.Subscribe(() =>
			{
				calls++;
				EventBus.EarlyCookRequested.Publish();
			});

			EventBus.EarlyCookRequested.Publish();

			Assert.AreEqual(1, calls);
			Assert.AreEqual(1, errors.Count);
			StringAssert.Contains("'EarlyCookRequested'", errors[0]);
		}

		[Test]
		public void SameChannelReentry_NoReporter_DoesNotThrowAndNotDispatched()
		{
			int calls = 0;
			EventBus.ModifiersChanged.Subscribe(() =>
			{
				calls++;
				EventBus.ModifiersChanged.Publish();
			});

			Assert.DoesNotThrow(() => EventBus.ModifiersChanged.Publish());
			Assert.AreEqual(1, calls);
		}

		[Test]
		public void SameChannelReentry_ChannelUsableAfterwards()
		{
			var errors = CaptureErrors();
			bool reenter = true;
			int calls = 0;
			EventBus.ModifiersChanged.Subscribe(() =>
			{
				calls++;
				if (reenter) EventBus.ModifiersChanged.Publish();
			});

			EventBus.ModifiersChanged.Publish();
			reenter = false;
			EventBus.ModifiersChanged.Publish();

			Assert.AreEqual(2, calls);
			Assert.AreEqual(1, errors.Count);
		}

		[Test]
		public void IndirectReentry_ThroughOtherChannel_IsDetected()
		{
			// GoldChanged → SaveRequested → GoldChanged：第二次 GoldChanged 仍在原派發中，應被擋下
			var errors = CaptureErrors();
			int goldCalls = 0;
			int saveCalls = 0;
			EventBus.GoldChanged.Subscribe(_ =>
			{
				goldCalls++;
				EventBus.SaveRequested.Publish(new SaveRequestedPayload("gold"));
			});
			EventBus.SaveRequested.Subscribe(_ =>
			{
				saveCalls++;
				EventBus.GoldChanged.Publish(new GoldChangedPayload(1, 1));
			});

			EventBus.GoldChanged.Publish(new GoldChangedPayload(0, 0));

			Assert.AreEqual(1, goldCalls);
			Assert.AreEqual(1, saveCalls);
			Assert.AreEqual(1, errors.Count);
			StringAssert.Contains("'GoldChanged'", errors[0]);
		}

		// ---- 跨頻道巢狀 ----

		[Test]
		public void CrossChannelNestedPublish_IsDispatched_WithoutError()
		{
			var errors = CaptureErrors();
			var order = new List<string>();
			EventBus.DishCompleted.Subscribe(_ =>
			{
				order.Add("dish:start");
				EventBus.GoldChanged.Publish(new GoldChangedPayload(50, 50));
				EventBus.SaveRequested.Publish(new SaveRequestedPayload("dish"));
				order.Add("dish:end");
			});
			bool outerDispatchingDuringNested = false;
			EventBus.GoldChanged.Subscribe(p =>
			{
				order.Add("gold:" + p.Delta);
				outerDispatchingDuringNested = EventBus.DishCompleted.IsDispatching;
			});
			EventBus.SaveRequested.Subscribe(p => order.Add("save:" + p.Reason));

			EventBus.DishCompleted.Publish(MakeRecord());

			CollectionAssert.AreEqual(new[] { "dish:start", "gold:50", "save:dish", "dish:end" }, order);
			Assert.IsTrue(outerDispatchingDuringNested);
			Assert.IsEmpty(errors);
			Assert.IsFalse(EventBus.DishCompleted.IsDispatching);
			Assert.IsFalse(EventBus.GoldChanged.IsDispatching);
		}

		// ---- 派發中取消 / 新增訂閱（快照語意） ----

		[Test]
		public void UnsubscribeDuringDispatch_DoesNotThrow_RemovedHandlerStillGetsCurrentEvent()
		{
			var errors = CaptureErrors();
			int secondCalls = 0;
			Action second = () => secondCalls++;
			Action first = () => EventBus.ModifiersChanged.Unsubscribe(second);
			EventBus.ModifiersChanged.Subscribe(first);
			EventBus.ModifiersChanged.Subscribe(second);

			Assert.DoesNotThrow(() => EventBus.ModifiersChanged.Publish());
			Assert.AreEqual(1, secondCalls, "Snapshot: handler removed mid-dispatch still receives the current event.");

			EventBus.ModifiersChanged.Publish();
			Assert.AreEqual(1, secondCalls, "Removed handler must not receive later events.");
			Assert.IsEmpty(errors);
		}

		[Test]
		public void UnsubscribeSelfDuringDispatch_DoesNotThrow_AndOthersStillReceive()
		{
			var errors = CaptureErrors();
			int selfCalls = 0;
			int otherCalls = 0;
			Action<string> self = null;
			self = _ =>
			{
				selfCalls++;
				EventBus.IngredientGenerated.Unsubscribe(self);
			};
			EventBus.IngredientGenerated.Subscribe(self);
			EventBus.IngredientGenerated.Subscribe(_ => otherCalls++);

			EventBus.IngredientGenerated.Publish("egg");
			EventBus.IngredientGenerated.Publish("egg");

			Assert.AreEqual(1, selfCalls);
			Assert.AreEqual(2, otherCalls);
			Assert.IsEmpty(errors);
		}

		[Test]
		public void SubscribeDuringDispatch_DoesNotThrow_NewHandlerOnlyGetsLaterEvents()
		{
			var errors = CaptureErrors();
			int lateCalls = 0;
			Action late = () => lateCalls++;
			EventBus.ModifiersChanged.Subscribe(() => EventBus.ModifiersChanged.Subscribe(late));

			Assert.DoesNotThrow(() => EventBus.ModifiersChanged.Publish());
			Assert.AreEqual(0, lateCalls, "Snapshot: handler added mid-dispatch must not receive the current event.");

			EventBus.ModifiersChanged.Publish();
			Assert.AreEqual(1, lateCalls);
			Assert.IsEmpty(errors);
		}

		// ---- 訂閱者拋例外 ----

		[Test]
		public void SubscriberThrows_OthersStillReceive_AndReported()
		{
			var errors = CaptureErrors();
			var received = new List<long>();
			EventBus.GoldChanged.Subscribe(_ => throw new InvalidOperationException("boom-from-subscriber"));
			EventBus.GoldChanged.Subscribe(p => received.Add(p.NewValue));

			Assert.DoesNotThrow(() => EventBus.GoldChanged.Publish(new GoldChangedPayload(7, 7)));

			CollectionAssert.AreEqual(new[] { 7L }, received);
			Assert.AreEqual(1, errors.Count);
			StringAssert.Contains("'GoldChanged'", errors[0]);
			StringAssert.Contains("boom-from-subscriber", errors[0]);
			StringAssert.Contains(nameof(InvalidOperationException), errors[0]);
			Assert.IsFalse(EventBus.GoldChanged.IsDispatching);
		}

		[Test]
		public void SubscriberThrows_ParameterlessChannel_OthersStillReceive_AndReported()
		{
			var errors = CaptureErrors();
			int calls = 0;
			EventBus.EarlyCookRequested.Subscribe(() => throw new InvalidOperationException("boom"));
			EventBus.EarlyCookRequested.Subscribe(() => calls++);

			Assert.DoesNotThrow(() => EventBus.EarlyCookRequested.Publish());

			Assert.AreEqual(1, calls);
			Assert.AreEqual(1, errors.Count);
			StringAssert.Contains("'EarlyCookRequested'", errors[0]);
		}

		[Test]
		public void SubscriberThrows_NoReporter_DoesNotThrow_OthersStillReceive()
		{
			int calls = 0;
			EventBus.ModifiersChanged.Subscribe(() => throw new InvalidOperationException("boom"));
			EventBus.ModifiersChanged.Subscribe(() => calls++);

			Assert.DoesNotThrow(() => EventBus.ModifiersChanged.Publish());
			Assert.AreEqual(1, calls);
		}

		[Test]
		public void ReporterItselfThrows_DispatchContinues()
		{
			EventBus.ErrorReporter = _ => throw new InvalidOperationException("reporter broke");
			int calls = 0;
			EventBus.ModifiersChanged.Subscribe(() => throw new InvalidOperationException("boom"));
			EventBus.ModifiersChanged.Subscribe(() => calls++);

			Assert.DoesNotThrow(() => EventBus.ModifiersChanged.Publish());
			Assert.AreEqual(1, calls);
			Assert.IsFalse(EventBus.ModifiersChanged.IsDispatching);
		}

		// ---- ResetAll ----

		[Test]
		public void ResetAll_ClearsSubscribersAndReporter()
		{
			int calls = 0;
			EventBus.ModifiersChanged.Subscribe(() => calls++);
			var gold = Capture(EventBus.GoldChanged);
			EventBus.ErrorReporter = _ => { };

			EventBus.ResetAll();
			EventBus.ModifiersChanged.Publish();
			EventBus.GoldChanged.Publish(new GoldChangedPayload(1, 1));

			Assert.AreEqual(0, calls);
			Assert.IsEmpty(gold);
			Assert.IsNull(EventBus.ErrorReporter);
			foreach (EventChannelBase channel in EventBus.Channels)
			{
				Assert.AreEqual(0, channel.SubscriberCount, channel.Name);
				Assert.IsFalse(channel.IsDispatching, channel.Name);
			}
		}

		[Test]
		public void ResetAll_DuringDispatch_ClearsDispatchingState()
		{
			bool dispatchingAfterReset = true;
			EventBus.ModifiersChanged.Subscribe(() =>
			{
				EventBus.ResetAll();
				dispatchingAfterReset = EventBus.ModifiersChanged.IsDispatching;
			});

			EventBus.ModifiersChanged.Publish();

			Assert.IsFalse(dispatchingAfterReset);
			Assert.IsFalse(EventBus.ModifiersChanged.IsDispatching);
		}

		[Test]
		public void ResetAll_CoversEveryChannelField()
		{
			// 防新增頻道漏掉：EventBus 上每個頻道欄位都必須已註冊，且 ResetAll 後無殘留訂閱
			var fields = new List<FieldInfo>(ChannelFields());
			Assert.AreEqual(11, fields.Count, "Technical spec 3.2 defines 11 events; update this count together with the spec.");
			Assert.AreEqual(fields.Count, EventBus.Channels.Count, "Every channel field must be registered (created via Register).");

			foreach (FieldInfo field in fields)
			{
				Assert.IsTrue(field.IsInitOnly, $"{field.Name} should be static readonly.");
				var channel = (EventChannelBase)field.GetValue(null);
				Assert.IsNotNull(channel, field.Name);
				CollectionAssert.Contains(EventBus.Channels, channel, $"{field.Name} is not registered.");

				SubscribeNoop(channel);
				Assert.AreEqual(1, channel.SubscriberCount, field.Name);
			}

			EventBus.ResetAll();

			foreach (FieldInfo field in fields)
			{
				var channel = (EventChannelBase)field.GetValue(null);
				Assert.AreEqual(0, channel.SubscriberCount, $"{field.Name} still has subscribers after ResetAll.");
			}
		}

		[Test]
		public void ChannelNames_MatchFieldNames_AndAreUnique()
		{
			var names = new HashSet<string>(StringComparer.Ordinal);
			foreach (FieldInfo field in ChannelFields())
			{
				var channel = (EventChannelBase)field.GetValue(null);
				Assert.AreEqual(field.Name, channel.Name);
				Assert.IsTrue(names.Add(channel.Name), $"Duplicate channel name '{channel.Name}'.");
			}
		}
	}
}
