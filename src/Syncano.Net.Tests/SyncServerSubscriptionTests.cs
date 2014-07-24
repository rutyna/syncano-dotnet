﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Should;
using Should.Core.Exceptions;
using Syncano.Net.DataRequests;
using SyncanoSyncServer.Net;
using SyncanoSyncServer.Net.Notifications;
using Xunit;

namespace Syncano.Net.Tests
{
    public class SyncServerSubscriptionTests
    {

        private SyncServer _syncSubscriber;

        private SyncServer _syncServer;

        public SyncServerSubscriptionTests()
        {
            if (!PrepareSyncServer().Wait(50000))
                throw new AssertException("Failed to initialize Syncano Sync Server client");
        }

        private async Task PrepareSyncServer()
        {
            _syncSubscriber = new SyncServer(TestData.InstanceName, TestData.BackendAdminApiKey);
            await _syncSubscriber.Start();


            _syncServer = new SyncServer(TestData.InstanceName, TestData.BackendAdminApiKey);
            await _syncServer.Start();
        }

        private static async Task WaitForNotifications()
        {
            await Task.Delay(1000);
        }

        [Fact]
        public async Task NewDataNotification_IsRecievedWhenNewData()
        {
            //given
            NewDataNotification newDataNotification = null;
            await
                _syncSubscriber.RealTimeSync.SubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            _syncSubscriber.NewDataObservable.Subscribe(m => newDataNotification = m);

            //when
            var newData = await _syncServer.DataObjects.New(new DataObjectDefinitionRequest()
            {
                ProjectId = TestData.ProjectId,
                CollectionId = TestData.SubscriptionCollectionId,
                Title = "test"
            });
            await WaitForNotifications();

            //then
            newDataNotification.ShouldNotBeNull();
            newDataNotification.Data.Title.ShouldEqual(newData.Title);
            newDataNotification.Data.Id.ShouldEqual(newData.Id);
            newDataNotification.Type.ShouldEqual(NotificationType.New);
            newDataNotification.Object.ShouldEqual(NotificationObject.Data);

            //cleanup
            await
                _syncSubscriber.RealTimeSync.UnsubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            await
                _syncServer.DataObjects.Delete(new DataObjectSimpleQueryRequest()
                {
                    ProjectId = TestData.ProjectId,
                    CollectionId = TestData.SubscriptionCollectionId
                });
        }

        [Fact]
        public async Task NewDataNotification_IsRecievedWhenNewData_MultipleNotifications()
        {
            //given
            var count = 10;
            var title = "Title";
            var notifications = new List<NewDataNotification>();
            await
                _syncSubscriber.RealTimeSync.SubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            _syncSubscriber.NewDataObservable.Subscribe(m => notifications.Add(m));

            //when
            for (int i = 0; i < count; ++i)
                await _syncServer.DataObjects.New(new DataObjectDefinitionRequest()
                {
                    ProjectId = TestData.ProjectId,
                    CollectionId = TestData.SubscriptionCollectionId,
                    Title = title,
                });

            await WaitForNotifications();

            //then
            notifications.Count.ShouldEqual(count);
            notifications.All(n => n.Data.Title == title).ShouldBeTrue();

            //cleanup
            await
                _syncSubscriber.RealTimeSync.UnsubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            await
                _syncServer.DataObjects.Delete(new DataObjectSimpleQueryRequest()
                {
                    ProjectId = TestData.ProjectId,
                    CollectionId = TestData.SubscriptionCollectionId
                });
        }

        [Fact]
        public async Task NewDataNotification_IsRecievedWhenNewData_MultipleNotifications_WithImages()
        {
            //given
            var count = 10;
            var notifications = new List<NewDataNotification>();
            await
                _syncSubscriber.RealTimeSync.SubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            _syncSubscriber.NewDataObservable.Subscribe(m => notifications.Add(m));

            //when
            for (int i = 0; i < count; ++i)
                await _syncServer.DataObjects.New(new DataObjectDefinitionRequest()
                {
                    ProjectId = TestData.ProjectId,
                    CollectionId = TestData.SubscriptionCollectionId,
                    Title = "test",
                    ImageBase64 = TestData.ImageToBase64("smallSampleImage.png")
                });

            await WaitForNotifications();

            //then
            notifications.Count.ShouldEqual(count);
            notifications.All(n => n.Data.Image != null).ShouldBeTrue();

            //cleanup
            await
                _syncSubscriber.RealTimeSync.UnsubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            await
                _syncServer.DataObjects.Delete(new DataObjectSimpleQueryRequest()
                {
                    ProjectId = TestData.ProjectId,
                    CollectionId = TestData.SubscriptionCollectionId
                });
        }

        [Fact]
        public async Task DeleteDataNotification_IsRecievedWhenDeleteData()
        {
            //given
            DeleteDataNotification deleteDataNotification = null;
            await
                _syncSubscriber.RealTimeSync.SubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            _syncSubscriber.DeleteDataObservable.Subscribe(m => deleteDataNotification = m);

            var newData = await _syncServer.DataObjects.New(new DataObjectDefinitionRequest()
            {
                ProjectId = TestData.ProjectId,
                CollectionId = TestData.SubscriptionCollectionId,
                Title = "test"
            });

            //given
            await
                _syncServer.DataObjects.Delete(new DataObjectSimpleQueryRequest()
                {
                    ProjectId = TestData.ProjectId,
                    CollectionId = TestData.SubscriptionCollectionId
                });

            await WaitForNotifications();

            //then
            deleteDataNotification.ShouldNotBeNull();
            deleteDataNotification.Target.ShouldNotBeNull();
            deleteDataNotification.Target.Ids.ShouldNotBeEmpty();
            deleteDataNotification.Target.Ids.Count.ShouldEqual(1);

            //cleanup
            await
                _syncSubscriber.RealTimeSync.UnsubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);

        }

        [Fact]
        public async Task ChangeDataNotification_IsRecievedWhenUpdateData()
        {
            //given
            ChangeDataNotification changeDataNotification = null;
            var newTitle = "newTitle";
            var newText = "newText";
            await
                _syncSubscriber.RealTimeSync.SubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            _syncSubscriber.ChangeDataObservable.Subscribe(m => changeDataNotification = m);

            var newData = await _syncServer.DataObjects.New(new DataObjectDefinitionRequest()
            {
                ProjectId = TestData.ProjectId,
                CollectionId = TestData.SubscriptionCollectionId,
                Title = "test"
            });

            //when
            await _syncServer.DataObjects.Update(
                new DataObjectDefinitionRequest()
                {
                    ProjectId = TestData.ProjectId,
                    CollectionId = TestData.SubscriptionCollectionId,
                    Title = newTitle,
                    Text = newText
                }, newData.Id);

            await WaitForNotifications();

            //then
            changeDataNotification.ShouldNotBeNull();

            //cleanup
            await
                _syncSubscriber.RealTimeSync.UnsubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            await
                _syncServer.DataObjects.Delete(new DataObjectSimpleQueryRequest()
                {
                    ProjectId = TestData.ProjectId,
                    CollectionId = TestData.SubscriptionCollectionId
                });
        }

        [Fact]
        public async Task ChangeDataNotification_IsRecievedWhenUpdateData_WithAdditionals()
        {
            //given
            ChangeDataNotification changeDataNotification = null;
            var newTitle = "newTitle";
            var newText = "newText";
            var additionals = new Dictionary<string, string>();
            additionals.Add("key", "value");

            await
                _syncSubscriber.RealTimeSync.SubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            _syncSubscriber.ChangeDataObservable.Subscribe(m => changeDataNotification = m);

            var newData = await _syncServer.DataObjects.New(new DataObjectDefinitionRequest()
            {
                ProjectId = TestData.ProjectId,
                CollectionId = TestData.SubscriptionCollectionId,
                Title = "test",
                Additional = additionals
            });

            additionals.Clear();
            additionals.Add("newKey", "newValue");

            //when
            await _syncServer.DataObjects.Update(
                new DataObjectDefinitionRequest()
                {
                    ProjectId = TestData.ProjectId,
                    CollectionId = TestData.SubscriptionCollectionId,
                    Title = newTitle,
                    Text = newText,
                    Additional = additionals
                }, newData.Id);

            await WaitForNotifications();

            //then
            changeDataNotification.ShouldNotBeNull();

            //cleanup
            await
                _syncSubscriber.RealTimeSync.UnsubscribeCollection(TestData.ProjectId,
                    collectionId: TestData.SubscriptionCollectionId);
            await
                _syncServer.DataObjects.Delete(new DataObjectSimpleQueryRequest()
                {
                    ProjectId = TestData.ProjectId,
                    CollectionId = TestData.SubscriptionCollectionId
                });
        }
    }
}